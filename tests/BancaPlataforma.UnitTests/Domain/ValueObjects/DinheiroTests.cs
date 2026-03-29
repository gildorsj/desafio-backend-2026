using BancaPlataforma.Domain.Exceptions;
using BancaPlataforma.Domain.ValueObjects;
using FluentAssertions;

namespace BancaPlataforma.UnitTests.Domain.ValueObjects;

public sealed class DinheiroTests
{
    [Fact]
    public void Create_ValorPositivo_DeveRetornarInstancia()
    {
        var dinheiro = Dinheiro.Create(100, "BRL");
        dinheiro.Valor.Should().Be(100);
        dinheiro.Moeda.Should().Be("BRL");
    }

    [Fact]
    public void Create_ValorNegativo_DeveLancarDomainException()
    {
        var act = () => Dinheiro.Create(-1, "BRL");
        act.Should().Throw<DomainException>()
            .WithMessage("*negativo*");
    }

    [Fact]
    public void Somar_MesmaMoeda_DeveRetornarSomaCorreta()
    {
        var a = Dinheiro.Create(100, "BRL");
        var b = Dinheiro.Create(50, "BRL");
        var resultado = a.Somar(b);
        resultado.Valor.Should().Be(150);
    }

    [Fact]
    public void Subtrair_SaldoSuficiente_DeveRetornarDiferenca()
    {
        var a = Dinheiro.Create(100, "BRL");
        var b = Dinheiro.Create(30, "BRL");
        var resultado = a.Subtrair(b);
        resultado.Valor.Should().Be(70);
    }

    [Fact]
    public void Subtrair_SaldoInsuficiente_DeveLancarDomainException()
    {
        var a = Dinheiro.Create(50, "BRL");
        var b = Dinheiro.Create(100, "BRL");
        var act = () => a.Subtrair(b);
        act.Should().Throw<DomainException>()
            .WithMessage("*Saldo insuficiente*");
    }

    [Fact]
    public void Somar_MoedasDiferentes_DeveLancarDomainException()
    {
        var brl = Dinheiro.Create(100, "BRL");
        var usd = Dinheiro.Create(100, "USD");
        var act = () => brl.Somar(usd);
        act.Should().Throw<DomainException>();
    }
}
