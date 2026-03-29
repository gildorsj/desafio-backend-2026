using MediatR;

namespace BancaPlataforma.Application.Accounts.Commands.Sacar;

public record SacarCommand(
    Guid ContaId,
    string IdempotencyKey,
    decimal Valor,
    string Moeda,
    string Descricao) : IRequest;
