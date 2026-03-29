using BancaPlataforma.Application.Common.Interfaces;
using BancaPlataforma.Domain.Enums;
using BancaPlataforma.Domain.Exceptions;
using MediatR;

namespace BancaPlataforma.Application.Accounts.Commands.AlterarStatusConta;

public sealed class AlterarStatusContaCommandHandler(
    IContaRepository repository,
    IUnitOfWork unitOfWork) : IRequestHandler<AlterarStatusContaCommand>
{
    public async Task Handle(AlterarStatusContaCommand request, CancellationToken ct)
    {
        var conta = await repository.ObterPorIdAsync(request.ContaId, ct)
                    ?? throw new DomainException("Conta não encontrada.");

        if (!Enum.TryParse<StatusConta>(request.Status, out var novoStatus))
            throw new DomainException($"Status inválido: {request.Status}.");

        conta.AlterarStatus(novoStatus);
        repository.Atualizar(conta);
        await unitOfWork.CommitAsync(ct);
    }
}
