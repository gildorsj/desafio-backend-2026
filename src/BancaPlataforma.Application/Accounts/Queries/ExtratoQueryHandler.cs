using BancaPlataforma.Application.Common.Interfaces;
using MediatR;

namespace BancaPlataforma.Application.Accounts.Queries;

public sealed class ExtratoQueryHandler(IContaReadRepository readRepository)
    : IRequestHandler<ExtratoQuery, ExtratoDto?>
{
    public Task<ExtratoDto?> Handle(ExtratoQuery request, CancellationToken ct) =>
        readRepository.ObterExtratoAsync(request, ct);
}
