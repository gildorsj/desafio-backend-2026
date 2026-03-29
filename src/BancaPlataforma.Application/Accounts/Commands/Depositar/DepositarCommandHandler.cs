using BancaPlataforma.Application.Common.Interfaces;
using BancaPlataforma.Domain.Exceptions;
using MediatR;

namespace BancaPlataforma.Application.Accounts.Commands.Depositar;

public sealed class DepositarCommandHandler(
    IContaRepository repository,
    IUnitOfWork unitOfWork,
    IIdempotencyService idempotency) : IRequestHandler<DepositarCommand>
{
    public async Task Handle(DepositarCommand request, CancellationToken ct)
    {
        if (await idempotency.ExisteAsync(request.IdempotencyKey, ct))
            return;
 
        if (await repository.ExisteTransacaoPorIdempotencyKeyAsync(request.IdempotencyKey, ct))
        {
            await idempotency.RegistrarAsync(request.IdempotencyKey, ct);
            return;
        }
 
        var conta = await repository.ObterPorIdAsync(request.ContaId, ct)
                    ?? throw new DomainException("Conta não encontrada.");
 
        conta.Depositar(request.Valor, request.Moeda, request.Descricao, request.IdempotencyKey);
 
        repository.Atualizar(conta);
        await unitOfWork.CommitAsync(ct);
        await idempotency.RegistrarAsync(request.IdempotencyKey, ct);
    }
}
