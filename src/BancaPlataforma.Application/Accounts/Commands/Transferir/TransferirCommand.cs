using MediatR;

namespace BancaPlataforma.Application.Accounts.Commands.Transferir;

public record TransferirCommand(
    Guid ContaOrigemId,
    string IdempotencyKey,
    Guid ContaDestinoId,
    decimal Valor,
    string Moeda,
    string Descricao) : IRequest;
