using BancaPlataforma.Application.Accounts.Queries;

namespace BancaPlataforma.Application.Common.Interfaces;

public interface IContaReadRepository
{
    Task<SaldoDto?> ObterSaldoAsync(Guid contaId, CancellationToken ct = default);
    Task<ExtratoDto?> ObterExtratoAsync(ExtratoQuery query, CancellationToken ct = default);
}
