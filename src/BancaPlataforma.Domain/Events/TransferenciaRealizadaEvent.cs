using BancaPlataforma.Domain.Primitives;

namespace BancaPlataforma.Domain.Events;

public sealed record TransferenciaRealizadaEvent(
    Guid ContaOrigemId,
    Guid ContaDestinoId,
    Guid TransacaoId,
    decimal Valor,
    string Moeda,
    string Descricao,
    string IdempotencyKey,
    DateTime OcorridoEm) : IDomainEvent;
