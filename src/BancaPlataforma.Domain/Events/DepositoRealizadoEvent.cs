using BancaPlataforma.Domain.Primitives;

namespace BancaPlataforma.Domain.Events;

public sealed record DepositoRealizadoEvent(
    Guid ContaId,
    Guid TransacaoId,
    decimal Valor,
    string Moeda,
    string Descricao,
    string IdempotencyKey,
    DateTime OcorridoEm) : IDomainEvent;
