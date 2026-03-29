using MediatR;

namespace BancaPlataforma.Application.Accounts.Commands.Depositar;

public record DepositarCommand(
    Guid ContaId,
    string IdempotencyKey,
    decimal Valor,
    string Moeda,
    string Descricao) : IRequest;
