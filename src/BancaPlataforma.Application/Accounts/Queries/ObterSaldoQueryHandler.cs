using BancaPlataforma.Application.Common.Interfaces;
using MediatR;

namespace BancaPlataforma.Application.Accounts.Queries;

public sealed class ObterSaldoQueryHandler(IContaReadRepository readRepository)
    : IRequestHandler<ObterSaldoQuery, SaldoDto?>
{
    public Task<SaldoDto?> Handle(ObterSaldoQuery request, CancellationToken ct) =>
        readRepository.ObterSaldoAsync(request.ContaId, ct);
}
