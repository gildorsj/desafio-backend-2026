using BancaPlataforma.Domain.Aggregates;
using BancaPlataforma.Domain.Entities;
using BancaPlataforma.Domain.Primitives;
using BancaPlataforma.Infrastructure.Audit;
using BancaPlataforma.Infrastructure.Outbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using System.Text.Json;

namespace BancaPlataforma.Infrastructure.Persistence;

public sealed class BancaDbContext(DbContextOptions<BancaDbContext> options) : DbContext(options)
{
    public DbSet<Conta> Contas => Set<Conta>();
    public DbSet<Transacao> Transacoes => Set<Transacao>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(BancaDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        // Captura auditoria antes do save para ter os valores originais disponíveis
        var auditorias = CapturarAuditoria();

        SerializarDomainEventsParaOutbox();

        if (auditorias.Count > 0)
            AuditLogs.AddRange(auditorias);

        return await base.SaveChangesAsync(ct);
    }

    // ──────────────────────────────────────────
    // Outbox
    // ──────────────────────────────────────────

    private void SerializarDomainEventsParaOutbox()
    {
        var aggregates = ChangeTracker
            .Entries<AggregateRoot>()
            .Select(e => e.Entity)
            .Where(a => a.DomainEvents.Any())
            .ToList();

        var mensagens = aggregates
            .SelectMany(a => a.DomainEvents)
            .Select(evento => new OutboxMessage
            {
                Id = Guid.NewGuid(),
                Tipo = evento.GetType().AssemblyQualifiedName!,
                Payload = JsonSerializer.Serialize(evento, evento.GetType()),
                CriadoEm = DateTime.UtcNow
            })
            .ToList();

        aggregates.ForEach(a => a.ClearDomainEvents());

        if (mensagens.Count > 0)
            OutboxMessages.AddRange(mensagens);
    }

    // ──────────────────────────────────────────
    // Auditoria
    // ──────────────────────────────────────────

    private List<AuditLog> CapturarAuditoria()
    {
        var entradas = ChangeTracker.Entries()
            .Where(e => e.Entity is not AuditLog
                     && e.Entity is not OutboxMessage
                     && e.State is EntityState.Added
                                or EntityState.Modified
                                or EntityState.Deleted)
            .ToList();

        var auditorias = new List<AuditLog>(entradas.Count);

        foreach (var entrada in entradas)
        {
            var operacao = entrada.State switch
            {
                EntityState.Added    => "Criado",
                EntityState.Modified => "Atualizado",
                EntityState.Deleted  => "Removido",
                _                    => string.Empty
            };

            var audit = new AuditLog
            {
                Id         = Guid.NewGuid(),
                Tabela     = entrada.Metadata.GetTableName() ?? entrada.Metadata.Name,
                EntidadeId = ObterEntidadeId(entrada),
                Operacao   = operacao,
                OcorridoEm = DateTime.UtcNow
            };

            if (entrada.State is EntityState.Modified or EntityState.Deleted)
                audit.ValoresAntigos = SerializarPropriedades(
                    entrada.Properties.Where(p => entrada.State == EntityState.Deleted || p.IsModified),
                    original: true);

            if (entrada.State is EntityState.Added or EntityState.Modified)
                audit.ValoresNovos = SerializarPropriedades(
                    entrada.Properties.Where(p => entrada.State == EntityState.Added || p.IsModified),
                    original: false);

            auditorias.Add(audit);
        }

        return auditorias;
    }

    private static string ObterEntidadeId(EntityEntry entrada)
    {
        var pkProps = entrada.Metadata.FindPrimaryKey()?.Properties;
        if (pkProps is null) return "desconhecido";

        var valores = pkProps.Select(p =>
        {
            var prop = entrada.Properties.FirstOrDefault(x => x.Metadata.Name == p.Name);
            return prop?.CurrentValue?.ToString()
                ?? prop?.OriginalValue?.ToString()
                ?? "null";
        });

        return string.Join(",", valores);
    }

    private static string SerializarPropriedades(
        IEnumerable<PropertyEntry> propriedades,
        bool original)
    {
        var dict = propriedades.ToDictionary(
            p => p.Metadata.Name,
            p => (object?)(original ? p.OriginalValue : p.CurrentValue));

        return JsonSerializer.Serialize(dict);
    }
}
