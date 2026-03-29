namespace BancaPlataforma.Application.Accounts.Queries;

public record ExtratoDto(
    Guid ContaId,
    IReadOnlyList<TransacaoDto> Transacoes,
    int Pagina,
    int TamanhoPagina,
    int TotalItens);

public record TransacaoDto(
    Guid Id,
    string Tipo,
    decimal Valor,
    string Moeda,
    string Descricao,
    Guid? ContaDestinoId,
    DateTime CriadoEm);
