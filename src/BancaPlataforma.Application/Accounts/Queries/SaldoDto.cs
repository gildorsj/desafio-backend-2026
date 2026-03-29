namespace BancaPlataforma.Application.Accounts.Queries;

public record SaldoDto(
    Guid ContaId,
    decimal Saldo,
    string Moeda,
    DateTime AtualizadoEm);
