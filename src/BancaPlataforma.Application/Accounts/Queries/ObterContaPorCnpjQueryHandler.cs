using BancaPlataforma.Application.Common.Interfaces;
using BancaPlataforma.Domain.Aggregates;
using MediatR;

namespace BancaPlataforma.Application.Accounts.Queries;

public sealed class ObterContaPorCnpjQueryHandler(IContaRepository repository)
    : IRequestHandler<ObterContaPorCnpjQuery, Conta?>
{
    public Task<Conta?> Handle(ObterContaPorCnpjQuery request, CancellationToken ct) =>
        repository.ObterPorCnpjAsync(request.Cnpj, ct);
}
