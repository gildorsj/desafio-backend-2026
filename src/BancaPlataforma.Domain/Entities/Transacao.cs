using BancaPlataforma.Domain.Enums;
using BancaPlataforma.Domain.Primitives;
using BancaPlataforma.Domain.ValueObjects;

namespace BancaPlataforma.Domain.Entities;

public sealed class Transacao : Entity
{
    public Guid ContaId { get; private set; }
    public TipoTransacao Tipo { get; private set; }
    public Dinheiro Valor { get; private set; }
    public string Descricao { get; private set; }
    public string IdempotencyKey { get; private set; }
    public Guid? ContaDestinoId { get; private set; }
    public DateTime CriadoEm { get; private set; }

    private Transacao() { } // EF Core

    private Transacao(
        Guid id,
        Guid contaId,
        TipoTransacao tipo,
        Dinheiro valor,
        string descricao,
        string idempotencyKey,
        Guid? contaDestinoId = null) : base(id)
    {
        ContaId = contaId;
        Tipo = tipo;
        Valor = valor;
        Descricao = descricao;
        IdempotencyKey = idempotencyKey;
        ContaDestinoId = contaDestinoId;
        CriadoEm = DateTime.UtcNow;
    }

    internal static Transacao Criar(
        Guid contaId,
        TipoTransacao tipo,
        Dinheiro valor,
        string descricao,
        string idempotencyKey,
        Guid? contaDestinoId = null) =>
        new(Guid.NewGuid(), contaId, tipo, valor, descricao, idempotencyKey, contaDestinoId);
}
