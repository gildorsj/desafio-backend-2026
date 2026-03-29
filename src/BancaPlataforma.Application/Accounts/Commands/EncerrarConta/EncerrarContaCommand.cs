using MediatR;

namespace BancaPlataforma.Application.Accounts.Commands.EncerrarConta;

public record EncerrarContaCommand(Guid ContaId) : IRequest;
