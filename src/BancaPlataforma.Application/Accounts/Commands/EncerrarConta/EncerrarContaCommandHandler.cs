using BancaPlataforma.Application.Common.Interfaces;
using BancaPlataforma.Domain.Exceptions;
using MediatR;

namespace BancaPlataforma.Application.Accounts.Commands.EncerrarConta;

public sealed class EncerrarContaCommandHandler(
    IContaRepository repository,
    IUnitOfWork unitOfWork) : IRequestHandler<EncerrarContaCommand>
{
    public async Task Handle(EncerrarContaCommand request, CancellationToken ct)
    {
        var conta = await repository.ObterPorIdAsync(request.ContaId, ct)
                    ?? throw new DomainException("Conta não encontrada.");

        conta.Encerrar();
        repository.Atualizar(conta);
        await unitOfWork.CommitAsync(ct);
    }
}
