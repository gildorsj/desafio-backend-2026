using BancaPlataforma.Domain.Primitives;

namespace BancaPlataforma.Domain.Events;

public sealed record ContaEncerradaEvent(
    Guid ContaId,
    DateTime OcorridoEm) : IDomainEvent;