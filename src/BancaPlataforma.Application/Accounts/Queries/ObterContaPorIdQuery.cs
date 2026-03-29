using BancaPlataforma.Domain.Aggregates;
using MediatR;

namespace BancaPlataforma.Application.Accounts.Queries;

public record ObterContaPorIdQuery(Guid Id) : IRequest<Conta?>;
