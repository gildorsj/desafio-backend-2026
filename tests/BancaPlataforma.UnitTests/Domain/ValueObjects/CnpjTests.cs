using BancaPlataforma.Domain.Exceptions;
using BancaPlataforma.Domain.ValueObjects;
using FluentAssertions;

namespace BancaPlataforma.UnitTests.Domain.ValueObjects;

public sealed class CnpjTests
{
    [Theory]
    [InlineData("33.000.167/0001-01")] // Petrobras
    [InlineData("33000167000101")]      // sem formatação
    [InlineData("60.746.948/0001-12")] // Bradesco
    public void Create_CnpjValido_DeveRetornarInstancia(string cnpj)
    {
        var act = () => Cnpj.Create(cnpj);
        act.Should().NotThrow();
    }

    [Theory]
    [InlineData("11.222.333/0001-82")] // inválido (dígito verificador errado)
    [InlineData("00.000.000/0000-00")] // zeros
    [InlineData("")]
    [InlineData("123")]
    public void Create_CnpjInvalido_DeveLancarDomainException(string cnpj)
    {
        var act = () => Cnpj.Create(cnpj);
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Create_CnpjFormatado_DeveNormalizarParaSoDigitos()
    {
        var cnpj = Cnpj.Create("33.000.167/0001-01");
        cnpj.Valor.Should().Be("33000167000101");
    }

    [Fact]
    public void Formatado_DeveRetornarCnpjFormatado()
    {
        var cnpj = Cnpj.Create("33000167000101");
        cnpj.Formatado.Should().Be("33.000.167/0001-01");
    }

    [Fact]
    public void Igualdade_MesmoCnpj_DeveSerIgual()
    {
        var cnpj1 = Cnpj.Create("33.000.167/0001-01");
        var cnpj2 = Cnpj.Create("33000167000101");
        cnpj1.Should().Be(cnpj2);
    }
}
