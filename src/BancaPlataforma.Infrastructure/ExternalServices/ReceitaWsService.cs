using BancaPlataforma.Application.Common.Interfaces;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace BancaPlataforma.Infrastructure.ExternalServices;

public sealed class ReceitaWsService(HttpClient httpClient) : IReceitaWsService
{
    public async Task<DadosCnpj?> ConsultarAsync(string cnpj, CancellationToken ct)
    {
        var response = await httpClient.GetAsync($"v1/cnpj/{cnpj}", ct);

        if (!response.IsSuccessStatusCode)
            return null;

        var json = await response.Content.ReadAsStringAsync(ct);

        // Proteção: se retornou HTML (limite da API), trata como falha
        if (json.TrimStart().StartsWith('<'))
            return null;

        var resultado = JsonSerializer.Deserialize<ReceitaWsResponse>(json);
        if (resultado is null) return null;

        return new DadosCnpj(resultado.Nome, resultado.Situacao);
    }

    private sealed class ReceitaWsResponse
    {
        [JsonPropertyName("nome")]
        public string Nome { get; set; } = string.Empty;

        [JsonPropertyName("situacao")]
        public string Situacao { get; set; } = string.Empty;
    }
}
