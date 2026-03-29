using BancaPlataforma.Domain.Exceptions;
using BancaPlataforma.Domain.Primitives;
using System.Text.RegularExpressions;

namespace BancaPlataforma.Domain.ValueObjects;

public sealed class Cnpj : ValueObject
{
    public string Valor { get; }

    private Cnpj(string valor) => Valor = valor;

    public static Cnpj Create(string cnpj)
    {
        var apenasDigitos = Regex.Replace(cnpj ?? "", @"[^\d]", "");

        if (!IsValido(apenasDigitos))
            throw new DomainException($"CNPJ inválido: {cnpj}");

        return new Cnpj(apenasDigitos);
    }

    // Retorna formatado: 11.222.333/0001-81
    public string Formatado =>
        $"{Valor[..2]}.{Valor[2..5]}.{Valor[5..8]}/{Valor[8..12]}-{Valor[12..]}";

    private static bool IsValido(string cnpj)
    {
        if (cnpj.Length != 14) return false;
        if (cnpj.Distinct().Count() == 1) return false; // ex: 00000000000000

        // Primeiro dígito verificador
        int[] mult1 = [5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2];
        var soma = cnpj.Take(12).Select((c, i) => (c - '0') * mult1[i]).Sum();
        var resto = soma % 11;
        var d1 = resto < 2 ? 0 : 11 - resto;

        // Segundo dígito verificador
        int[] mult2 = [6, 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2];
        soma = cnpj.Take(13).Select((c, i) => (c - '0') * mult2[i]).Sum();
        resto = soma % 11;
        var d2 = resto < 2 ? 0 : 11 - resto;

        return cnpj[12] - '0' == d1 && cnpj[13] - '0' == d2;
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Valor;
    }

    public override string ToString() => Formatado;
}
