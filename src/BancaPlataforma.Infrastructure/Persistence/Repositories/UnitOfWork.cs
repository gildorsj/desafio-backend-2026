using BancaPlataforma.Application.Common.Interfaces;

namespace BancaPlataforma.Infrastructure.Persistence.Repositories;

public sealed class UnitOfWork(BancaDbContext context) : IUnitOfWork
{
    public Task<int> CommitAsync(CancellationToken ct) =>
        context.SaveChangesAsync(ct);
}
