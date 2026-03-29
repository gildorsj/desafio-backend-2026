using BancaPlataforma.Application.Common.Interfaces;
using BancaPlataforma.Domain.Aggregates;
using MediatR;

namespace BancaPlataforma.Application.Accounts.Queries;

public sealed class ObterContaPorIdQueryHandler(IContaRepository repository)
    : IRequestHandler<ObterContaPorIdQuery, Conta?>
{
    public Task<Conta?> Handle(ObterContaPorIdQuery request, CancellationToken ct) =>
        repository.ObterPorIdAsync(request.Id, ct);
}
