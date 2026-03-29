using BancaPlataforma.Domain.Aggregates;
using BancaPlataforma.Domain.Enums;
using BancaPlataforma.Domain.Events;
using BancaPlataforma.Domain.Exceptions;
using BancaPlataforma.Domain.ValueObjects;
using FluentAssertions;

namespace BancaPlataforma.UnitTests.Domain.Aggregates;

public sealed class ContaTests
{
    private static Conta CriarContaValida() => Conta.Abrir(
        Cnpj.Create("33.000.167/0001-01"),
        "Petróleo Brasileiro S.A.",
        "0001",
        "base64imagem");

    // ── Abertura ─────────────────────────────────────────────────

    [Fact]
    public void Abrir_DadosValidos_DeveCriarContaAtiva()
    {
        var conta = CriarContaValida();

        conta.Status.Should().Be(StatusConta.Ativa);
        conta.Saldo.Valor.Should().Be(0);
        conta.DomainEvents.Should().ContainSingle(e => e is ContaAbertaEvent);
    }

    // ── Depósito ─────────────────────────────────────────────────

    [Fact]
    public void Depositar_ContaAtiva_DeveAtualizarSaldo()
    {
        var conta = CriarContaValida();
        conta.Depositar(500, "BRL", "Depósito teste", "key-001");

        conta.Saldo.Valor.Should().Be(500);
        conta.DomainEvents.Should().Contain(e => e is DepositoRealizadoEvent);
    }

    [Fact]
    public void Depositar_ContaBloqueada_DeveLancarDomainException()
    {
        var conta = CriarContaValida();
        conta.AlterarStatus(StatusConta.Bloqueada);

        var act = () => conta.Depositar(500, "BRL", "Teste", "key-001");
        act.Should().Throw<DomainException>().WithMessage("*bloqueada*");
    }

    [Fact]
    public void Depositar_ContaEncerrada_DeveLancarDomainException()
    {
        var conta = CriarContaValida();
        conta.Encerrar();

        var act = () => conta.Depositar(500, "BRL", "Teste", "key-001");
        act.Should().Throw<DomainException>().WithMessage("*encerrada*");
    }

    // ── Saque ────────────────────────────────────────────────────

    [Fact]
    public void Sacar_SaldoSuficiente_DeveAtualizarSaldo()
    {
        var conta = CriarContaValida();
        conta.Depositar(1000, "BRL", "Depósito", "key-dep");
        conta.Sacar(300, "BRL", "Saque teste", "key-saq");

        conta.Saldo.Valor.Should().Be(700);
        conta.DomainEvents.Should().Contain(e => e is SaqueRealizadoEvent);
    }

    [Fact]
    public void Sacar_SaldoInsuficiente_DeveLancarDomainException()
    {
        var conta = CriarContaValida();
        conta.Depositar(100, "BRL", "Depósito", "key-dep");

        var act = () => conta.Sacar(500, "BRL", "Saque", "key-saq");
        act.Should().Throw<DomainException>().WithMessage("*Saldo insuficiente*");
    }

    // ── Transferência ────────────────────────────────────────────

    [Fact]
    public void Transferir_ContasAtivas_DeveDebitarOrigemECreditarDestino()
    {
        var origem = CriarContaValida();
        var destino = Conta.Abrir(
            Cnpj.Create("60.746.948/0001-12"),
            "Bradesco S.A.",
            "0001",
            "base64");

        origem.Depositar(1000, "BRL", "Depósito", "key-dep");
        origem.Transferir(destino, 400, "BRL", "Transferência", "key-trf");

        origem.Saldo.Valor.Should().Be(600);
        destino.Saldo.Valor.Should().Be(400);
        origem.DomainEvents.Should().Contain(e => e is TransferenciaRealizadaEvent);
    }

    [Fact]
    public void Transferir_ContaDestinoInativa_DeveLancarDomainException()
    {
        var origem = CriarContaValida();
        var destino = Conta.Abrir(
            Cnpj.Create("60.746.948/0001-12"),
            "Bradesco S.A.",
            "0001",
            "base64");

        origem.Depositar(1000, "BRL", "Depósito", "key-dep");
        destino.AlterarStatus(StatusConta.Bloqueada);

        var act = () => origem.Transferir(destino, 400, "BRL", "Transferência", "key-trf");
        act.Should().Throw<DomainException>().WithMessage("*destino*");
    }

    // ── Encerramento ─────────────────────────────────────────────

    [Fact]
    public void Encerrar_SaldoZero_DeveAlterarStatusParaEncerrada()
    {
        var conta = CriarContaValida();
        conta.Encerrar();

        conta.Status.Should().Be(StatusConta.Encerrada);
        conta.DomainEvents.Should().Contain(e => e is ContaEncerradaEvent);
    }

    [Fact]
    public void Encerrar_ComSaldo_DeveLancarDomainException()
    {
        var conta = CriarContaValida();
        conta.Depositar(100, "BRL", "Depósito", "key-dep");

        var act = () => conta.Encerrar();
        act.Should().Throw<DomainException>().WithMessage("*saldo igual a zero*");
    }

    // ── Status ───────────────────────────────────────────────────

    [Fact]
    public void AlterarStatus_ContaEncerrada_DeveLancarDomainException()
    {
        var conta = CriarContaValida();
        conta.Encerrar();

        var act = () => conta.AlterarStatus(StatusConta.Ativa);
        act.Should().Throw<DomainException>().WithMessage("*Conta encerrada*");
    }

    [Fact]
    public void AlterarStatus_MesmoStatus_DeveLancarDomainException()
    {
        var conta = CriarContaValida();

        var act = () => conta.AlterarStatus(StatusConta.Ativa);
        act.Should().Throw<DomainException>().WithMessage("*já está*");
    }
}