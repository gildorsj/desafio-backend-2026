using MediatR;

namespace BancaPlataforma.Application.Accounts.Commands.AlterarStatusConta;

public record AlterarStatusContaCommand(Guid ContaId, string Status) : IRequest;
