namespace BancaPlataforma.Application.Common.Interfaces;

public record DadosCnpj(string RazaoSocial, string Situacao);

public interface IReceitaWsService
{
    Task<DadosCnpj?> ConsultarAsync(string cnpj, CancellationToken ct = default);
}
