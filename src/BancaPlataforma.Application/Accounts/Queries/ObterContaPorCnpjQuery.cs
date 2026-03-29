using BancaPlataforma.Domain.Aggregates;
using MediatR;

namespace BancaPlataforma.Application.Accounts.Queries;

public record ObterContaPorCnpjQuery(string Cnpj) : IRequest<Conta?>;
