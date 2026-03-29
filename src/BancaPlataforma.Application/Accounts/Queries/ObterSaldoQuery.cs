using MediatR;

namespace BancaPlataforma.Application.Accounts.Queries;

public record ObterSaldoQuery(Guid ContaId) : IRequest<SaldoDto?>;
