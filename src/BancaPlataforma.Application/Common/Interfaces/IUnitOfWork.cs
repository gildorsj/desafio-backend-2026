namespace BancaPlataforma.Application.Common.Interfaces;

public interface IUnitOfWork
{
    Task<int> CommitAsync(CancellationToken ct = default);
}
