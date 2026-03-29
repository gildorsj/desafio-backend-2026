using BancaPlataforma.Application.Common.Interfaces;
using BancaPlataforma.Domain.Exceptions;
using MediatR;

namespace BancaPlataforma.Application.Accounts.Commands.Sacar;

public sealed class SacarCommandHandler(
    IContaRepository repository,
    IUnitOfWork unitOfWork,
    IIdempotencyService idempotency) : IRequestHandler<SacarCommand>
{
    public async Task Handle(SacarCommand request, CancellationToken ct)
    {
        if (await idempotency.ExisteAsync(request.IdempotencyKey, ct))
            return;

        var conta = await repository.ObterPorIdAsync(request.ContaId, ct)
                    ?? throw new DomainException("Conta não encontrada.");

        conta.Sacar(request.Valor, request.Moeda, request.Descricao, request.IdempotencyKey);

        repository.Atualizar(conta);
        await unitOfWork.CommitAsync(ct);
        await idempotency.RegistrarAsync(request.IdempotencyKey, ct);
    }
}
