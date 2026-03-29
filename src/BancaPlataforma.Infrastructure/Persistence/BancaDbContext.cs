using BancaPlataforma.Domain.Aggregates;
using BancaPlataforma.Domain.Entities;
using BancaPlataforma.Domain.Primitives;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BancaPlataforma.Infrastructure.Persistence;

public sealed class BancaDbContext(
    DbContextOptions<BancaDbContext> options,
    IPublisher publisher) : DbContext(options)
{
    public DbSet<Conta> Contas => Set<Conta>();
    public DbSet<Transacao> Transacoes => Set<Transacao>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(BancaDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }

    // Publica domain events após salvar no banco
    public override async Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        var result = await base.SaveChangesAsync(ct);
        await PublicarDomainEventsAsync(ct);
        return result;
    }

    private async Task PublicarDomainEventsAsync(CancellationToken ct)
    {
        var aggregates = ChangeTracker
            .Entries<AggregateRoot>()
            .Select(e => e.Entity)
            .Where(a => a.DomainEvents.Any())
            .ToList();

        var events = aggregates
            .SelectMany(a => a.DomainEvents)
            .ToList();

        aggregates.ForEach(a => a.ClearDomainEvents());

        foreach (var domainEvent in events)
            await publisher.Publish(domainEvent, ct);
    }
}
