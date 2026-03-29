using BancaPlataforma.Application.Common.Interfaces;
using BancaPlataforma.Domain.Aggregates;
using BancaPlataforma.Domain.Exceptions;
using BancaPlataforma.Domain.ValueObjects;
using MediatR;

namespace BancaPlataforma.Application.Accounts.Commands.AbrirConta;

public sealed class AbrirContaCommandHandler(
    IContaRepository repository,
    IUnitOfWork unitOfWork,
    IReceitaWsService receitaWs) : IRequestHandler<AbrirContaCommand, Guid>
{
    public async Task<Guid> Handle(AbrirContaCommand request, CancellationToken ct)
    {
        var cnpj = Cnpj.Create(request.Cnpj);

        if (await repository.ExistePorCnpjAsync(cnpj.Valor, ct))
            throw new DomainException("Já existe uma conta para este CNPJ.");

        var dados = await receitaWs.ConsultarAsync(cnpj.Valor, ct)
                    ?? throw new DomainException("Não foi possível consultar o CNPJ na Receita Federal. Verifique o CNPJ ou tente novamente.");
        
        if (!dados.Situacao.Equals("ATIVA", StringComparison.OrdinalIgnoreCase))
            throw new DomainException("CNPJ não está ativo na Receita Federal.");

        var conta = Conta.Abrir(cnpj, dados.RazaoSocial, request.Agencia, request.ImagemDocumento);

        await repository.AdicionarAsync(conta, ct);
        await unitOfWork.CommitAsync(ct);

        return conta.Id;
    }
}
