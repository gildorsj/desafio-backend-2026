namespace BancaPlataforma.Application.Common.Interfaces;

public interface IIdempotencyService
{
    Task<bool> ExisteAsync(string key, CancellationToken ct = default);
    Task RegistrarAsync(string key, CancellationToken ct = default);
}
