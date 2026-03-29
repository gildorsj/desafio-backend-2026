using MediatR;

namespace BancaPlataforma.Application.Accounts.Queries;

public record ExtratoQuery(
    Guid ContaId,
    DateTime? DataInicio,
    DateTime? DataFim,
    string? Tipo,
    int Pagina = 1,
    int TamanhoPagina = 20) : IRequest<ExtratoDto?>;
