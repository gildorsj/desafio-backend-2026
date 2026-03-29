using BancaPlataforma.Domain.Aggregates;

namespace BancaPlataforma.Application.Common.Interfaces;

public interface IContaRepository
{
    Task<Conta?> ObterPorIdAsync(Guid id, CancellationToken ct = default);
    Task<Conta?> ObterPorCnpjAsync(string cnpj, CancellationToken ct = default);
    Task<bool> ExistePorCnpjAsync(string cnpj, CancellationToken ct = default);
    Task AdicionarAsync(Conta conta, CancellationToken ct = default);
    void Atualizar(Conta conta);
}
