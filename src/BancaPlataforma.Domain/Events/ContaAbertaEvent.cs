using BancaPlataforma.Domain.Primitives;

namespace BancaPlataforma.Domain.Events;

public sealed record ContaAbertaEvent(
    Guid ContaId,
    string Cnpj,
    string RazaoSocial,
    string Agencia,
    DateTime OcorridoEm) : IDomainEvent;
