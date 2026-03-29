namespace BancaPlataforma.API.Models;

public sealed record AbrirContaRequest(
    string Cnpj,
    string Agencia,
    string ImagemDocumento);

public sealed record AlterarStatusRequest(string Status);

public sealed record OperacaoFinanceiraRequest(
    string IdempotencyKey,
    decimal Valor,
    string Moeda,
    string Descricao);

public sealed record TransferenciaRequest(
    string IdempotencyKey,
    Guid ContaDestinoId,
    decimal Valor,
    string Moeda,
    string Descricao);
