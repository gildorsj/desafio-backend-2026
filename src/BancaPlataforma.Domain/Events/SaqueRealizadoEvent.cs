using BancaPlataforma.Domain.Primitives;

namespace BancaPlataforma.Domain.Events;

public sealed record SaqueRealizadoEvent(
    Guid ContaId,
    Guid TransacaoId,
    decimal Valor,
    string Moeda,
    string Descricao,
    string IdempotencyKey,
    DateTime OcorridoEm) : IDomainEvent;
