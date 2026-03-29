using BancaPlataforma.Domain.Enums;
using BancaPlataforma.Domain.Primitives;

namespace BancaPlataforma.Domain.Events;

public sealed record StatusContaAlteradoEvent(
    Guid ContaId,
    StatusConta StatusAnterior,
    StatusConta StatusNovo,
    DateTime OcorridoEm) : IDomainEvent;
