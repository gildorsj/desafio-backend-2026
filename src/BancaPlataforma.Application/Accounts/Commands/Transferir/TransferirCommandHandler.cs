using BancaPlataforma.Application.Common.Interfaces;
using BancaPlataforma.Domain.Exceptions;
using MediatR;

namespace BancaPlataforma.Application.Accounts.Commands.Transferir;

public sealed class TransferirCommandHandler(
    IContaRepository repository,
    IUnitOfWork unitOfWork,
    IIdempotencyService idempotency) : IRequestHandler<TransferirCommand>
{
    public async Task Handle(TransferirCommand request, CancellationToken ct)
    {
        if (await idempotency.ExisteAsync(request.IdempotencyKey, ct))
            return;

        var contaOrigem = await repository.ObterPorIdAsync(request.ContaOrigemId, ct)
                          ?? throw new DomainException("Conta de origem não encontrada.");

        var contaDestino = await repository.ObterPorIdAsync(request.ContaDestinoId, ct)
                           ?? throw new DomainException("Conta de destino não encontrada.");

        contaOrigem.Transferir(contaDestino, request.Valor, request.Moeda, request.Descricao, request.IdempotencyKey);

        repository.Atualizar(contaOrigem);
        repository.Atualizar(contaDestino);
        await unitOfWork.CommitAsync(ct);
        await idempotency.RegistrarAsync(request.IdempotencyKey, ct);
    }
}
