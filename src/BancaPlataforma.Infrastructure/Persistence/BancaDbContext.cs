using BancaPlataforma.Domain.Aggregates;
using BancaPlataforma.Domain.Entities;
using BancaPlataforma.Domain.Primitives;
using BancaPlataforma.Infrastructure.Outbox;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace BancaPlataforma.Infrastructure.Persistence;

public sealed class BancaDbContext(DbContextOptions<BancaDbContext> options) : DbContext(options)
{
    public DbSet<Conta> Contas => Set<Conta>();
    public DbSet<Transacao> Transacoes => Set<Transacao>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(BancaDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        SerializarDomainEventsParaOutbox();
        return await base.SaveChangesAsync(ct);
    }

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
}
