using MediatR;

namespace BancaPlataforma.Application.Accounts.Commands.AbrirConta;

public record AbrirContaCommand(
    string Cnpj,
    string Agencia,
    string ImagemDocumento) : IRequest<Guid>;
