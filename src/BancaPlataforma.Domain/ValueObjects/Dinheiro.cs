using BancaPlataforma.Domain.Exceptions;
using BancaPlataforma.Domain.Primitives;

namespace BancaPlataforma.Domain.ValueObjects;

public sealed class Dinheiro : ValueObject
{
    public decimal Valor { get; }
    public string Moeda { get; }

    private Dinheiro(decimal valor, string moeda)
    {
        Valor = valor;
        Moeda = moeda;
    }

    public static Dinheiro Create(decimal valor, string moeda)
    {
        if (valor < 0)
            throw new DomainException("O valor monetário não pode ser negativo.");

        if (string.IsNullOrWhiteSpace(moeda))
            throw new DomainException("A moeda é obrigatória.");

        return new Dinheiro(valor, moeda.ToUpperInvariant());
    }

    public static Dinheiro Zero(string moeda = "BRL") => new(0, moeda);

    public Dinheiro Somar(Dinheiro outro)
    {
        ValidarMesma(outro);
        return new Dinheiro(Valor + outro.Valor, Moeda);
    }

    public Dinheiro Subtrair(Dinheiro outro)
    {
        ValidarMesma(outro);
        if (Valor < outro.Valor)
            throw new DomainException("Saldo insuficiente.");
        return new Dinheiro(Valor - outro.Valor, Moeda);
    }

    private void ValidarMesma(Dinheiro outro)
    {
        if (Moeda != outro.Moeda)
            throw new DomainException($"Não é possível operar moedas diferentes: {Moeda} e {outro.Moeda}.");
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Valor;
        yield return Moeda;
    }

    public override string ToString() => $"{Valor:F2} {Moeda}";
}
