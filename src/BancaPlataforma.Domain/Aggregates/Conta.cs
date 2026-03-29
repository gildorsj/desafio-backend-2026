using BancaPlataforma.Domain.Events;
using BancaPlataforma.Domain.Entities;
using BancaPlataforma.Domain.Enums;
using BancaPlataforma.Domain.Exceptions;
using BancaPlataforma.Domain.Primitives;
using BancaPlataforma.Domain.ValueObjects;

namespace BancaPlataforma.Domain.Aggregates;

public sealed class Conta : AggregateRoot
{
    private readonly List<Transacao> _transacoes = [];

    public Cnpj Cnpj { get; private set; }
    public string RazaoSocial { get; private set; }
    public string Agencia { get; private set; }
    public string ImagemDocumentoBase64 { get; private set; }
    public Dinheiro Saldo { get; private set; }
    public StatusConta Status { get; private set; }
    public DateTime CriadoEm { get; private set; }
    public DateTime? AtualizadoEm { get; private set; }

    public IReadOnlyList<Transacao> Transacoes => _transacoes.AsReadOnly();

    private Conta() { } // EF Core
    

    private Conta(
        Guid id,
        Cnpj cnpj,
        string razaoSocial,
        string agencia,
        string imagemDocumentoBase64) : base(id)
    {
        Cnpj = cnpj;
        RazaoSocial = razaoSocial;
        Agencia = agencia;
        ImagemDocumentoBase64 = imagemDocumentoBase64;
        Saldo = Dinheiro.Zero();
        Status = StatusConta.Ativa;
        CriadoEm = DateTime.UtcNow;
    }

    // ──────────────────────────────────────────
    // Factory
    // ──────────────────────────────────────────

    public static Conta Abrir(
        Cnpj cnpj,
        string razaoSocial,
        string agencia,
        string imagemDocumentoBase64)
    {
        var conta = new Conta(Guid.NewGuid(), cnpj, razaoSocial, agencia, imagemDocumentoBase64);

        conta.RaiseDomainEvent(new ContaAbertaEvent(
            conta.Id,
            cnpj.Valor,
            razaoSocial,
            agencia,
            conta.CriadoEm));

        return conta;
    }

    // ──────────────────────────────────────────
    // Comportamentos
    // ──────────────────────────────────────────

    public void AlterarStatus(StatusConta novoStatus)
    {
        if (Status == StatusConta.Encerrada)
            throw new DomainException("Conta encerrada não pode ter o status alterado.");

        if (Status == novoStatus)
            throw new DomainException($"A conta já está com o status {novoStatus}.");

        var statusAnterior = Status;
        Status = novoStatus;
        AtualizadoEm = DateTime.UtcNow;

        RaiseDomainEvent(new StatusContaAlteradoEvent(Id, statusAnterior, novoStatus, AtualizadoEm.Value));
    }

    public void Encerrar()
    {
        if (Status == StatusConta.Encerrada)
            throw new DomainException("Conta já está encerrada.");

        if (Saldo.Valor != 0)
            throw new DomainException("Só é possível encerrar conta com saldo igual a zero.");

        Status = StatusConta.Encerrada;
        AtualizadoEm = DateTime.UtcNow;

        RaiseDomainEvent(new ContaEncerradaEvent(Id, AtualizadoEm.Value));
    }

    public Transacao Depositar(decimal valor, string moeda, string descricao, string idempotencyKey)
    {
        ValidarContaAtiva("depósito");

        var dinheiro = Dinheiro.Create(valor, moeda);
        Saldo = Saldo.Somar(dinheiro);
        AtualizadoEm = DateTime.UtcNow;

        var transacao = Transacao.Criar(Id, TipoTransacao.Deposito, dinheiro, descricao, idempotencyKey);
        _transacoes.Add(transacao);

        RaiseDomainEvent(new DepositoRealizadoEvent(
            Id, transacao.Id, valor, moeda, descricao, idempotencyKey, AtualizadoEm.Value));

        return transacao;
    }

    public Transacao Sacar(decimal valor, string moeda, string descricao, string idempotencyKey)
    {
        ValidarContaAtiva("saque");

        var dinheiro = Dinheiro.Create(valor, moeda);
        Saldo = Saldo.Subtrair(dinheiro); // lança DomainException se saldo insuficiente
        AtualizadoEm = DateTime.UtcNow;

        var transacao = Transacao.Criar(Id, TipoTransacao.Saque, dinheiro, descricao, idempotencyKey);
        _transacoes.Add(transacao);

        RaiseDomainEvent(new SaqueRealizadoEvent(
            Id, transacao.Id, valor, moeda, descricao, idempotencyKey, AtualizadoEm.Value));

        return transacao;
    }

    public Transacao Transferir(
        Conta contaDestino,
        decimal valor,
        string moeda,
        string descricao,
        string idempotencyKey)
    {
        ValidarContaAtiva("transferência");

        if (contaDestino.Status != StatusConta.Ativa)
            throw new DomainException("A conta de destino deve estar ativa para receber transferências.");

        var dinheiro = Dinheiro.Create(valor, moeda);

        // Débito na origem
        Saldo = Saldo.Subtrair(dinheiro);
        AtualizadoEm = DateTime.UtcNow;

        // Crédito no destino
        contaDestino.Saldo = contaDestino.Saldo.Somar(dinheiro);
        contaDestino.AtualizadoEm = DateTime.UtcNow;

        var transacao = Transacao.Criar(
            Id, TipoTransacao.Transferencia, dinheiro, descricao, idempotencyKey, contaDestino.Id);
        _transacoes.Add(transacao);

        RaiseDomainEvent(new TransferenciaRealizadaEvent(
            Id, contaDestino.Id, transacao.Id, valor, moeda, descricao, idempotencyKey, AtualizadoEm.Value));

        return transacao;
    }

    // ──────────────────────────────────────────
    // Helpers privados
    // ──────────────────────────────────────────

    private void ValidarContaAtiva(string operacao)
    {
        if (Status == StatusConta.Bloqueada)
            throw new DomainException($"Conta bloqueada não aceita {operacao}.");

        if (Status == StatusConta.Encerrada)
            throw new DomainException($"Conta encerrada não aceita {operacao}.");
    }
}