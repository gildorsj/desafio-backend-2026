using System.Net;
using System.Net.Http.Json;
using FluentAssertions;

namespace BancaPlataforma.IntegrationTests.Accounts;

public sealed class AccountsEndpointTests : IClassFixture<BancaPlataformaWebFactory>
{
    private readonly HttpClient _client;

    public AccountsEndpointTests(BancaPlataformaWebFactory factory)
    {
        _client = factory.CreateClient();
    }

    // ── GET /health ───────────────────────────────────────────────

    [Fact]
    public async Task GET_Health_DeveResponder()
    {
        var response = await _client.GetAsync("/health");
        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.OK,
            HttpStatusCode.ServiceUnavailable);
    }

    // ── POST /accounts ────────────────────────────────────────────

    [Fact]
    public async Task POST_AbrirConta_CnpjInvalido_DeveRetornar422()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/accounts", new
        {
            cnpj = "00.000.000/0000-00",
            agencia = "0001",
            imagemDocumento = "dGVzdGU="
        });

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task POST_AbrirConta_CamposObrigatoriosFaltando_DeveRetornar400()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/accounts", new
        {
            cnpj = "",
            agencia = "",
            imagemDocumento = ""
        });

        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.BadRequest,
            HttpStatusCode.UnprocessableEntity);
    }

    // ── GET /accounts/{id} ────────────────────────────────────────

    [Fact]
    public async Task GET_ObterContaPorId_IdInexistente_DeveRetornar404()
    {
        var response = await _client.GetAsync($"/api/v1/accounts/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── GET /accounts/cnpj/{cnpj} ─────────────────────────────────

    [Fact]
    public async Task GET_ObterContaPorCnpj_CnpjInexistente_DeveRetornar404()
    {
        var response = await _client.GetAsync("/api/v1/accounts/cnpj/33000167000101");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── PATCH /accounts/{id}/status ───────────────────────────────

    [Fact]
    public async Task PATCH_AlterarStatus_ContaInexistente_DeveRetornar422()
    {
        var response = await _client.PatchAsJsonAsync(
            $"/api/v1/accounts/{Guid.NewGuid()}/status",
            new { status = "Bloqueada" });

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task PATCH_AlterarStatus_StatusInvalido_DeveRetornar422()
    {
        var response = await _client.PatchAsJsonAsync(
            $"/api/v1/accounts/{Guid.NewGuid()}/status",
            new { status = "StatusInexistente" });

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    // ── DELETE /accounts/{id} ─────────────────────────────────────

    [Fact]
    public async Task DELETE_EncerrarConta_ContaInexistente_DeveRetornar422()
    {
        var response = await _client.DeleteAsync($"/api/v1/accounts/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    // ── POST /accounts/{id}/deposit ───────────────────────────────

    [Fact]
    public async Task POST_Depositar_ContaInexistente_DeveRetornar422()
    {
        var response = await _client.PostAsJsonAsync(
            $"/api/v1/accounts/{Guid.NewGuid()}/deposit",
            new
            {
                idempotencyKey = Guid.NewGuid().ToString(),
                valor = 100.00,
                moeda = "BRL",
                descricao = "Depósito teste"
            });

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    // ── POST /accounts/{id}/withdraw ──────────────────────────────

    [Fact]
    public async Task POST_Sacar_ContaInexistente_DeveRetornar422()
    {
        var response = await _client.PostAsJsonAsync(
            $"/api/v1/accounts/{Guid.NewGuid()}/withdraw",
            new
            {
                idempotencyKey = Guid.NewGuid().ToString(),
                valor = 100.00,
                moeda = "BRL",
                descricao = "Saque teste"
            });

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    // ── POST /accounts/{id}/transfer ──────────────────────────────

    [Fact]
    public async Task POST_Transferir_ContaInexistente_DeveRetornar422()
    {
        var response = await _client.PostAsJsonAsync(
            $"/api/v1/accounts/{Guid.NewGuid()}/transfer",
            new
            {
                idempotencyKey = Guid.NewGuid().ToString(),
                contaDestinoId = Guid.NewGuid(),
                valor = 100.00,
                moeda = "BRL",
                descricao = "Transferência teste"
            });

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    // ── GET /accounts/{id}/balance ────────────────────────────────

    [Fact]
    public async Task GET_Saldo_ContaInexistente_DeveRetornar404()
    {
        var response = await _client.GetAsync($"/api/v1/accounts/{Guid.NewGuid()}/balance");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── GET /accounts/{id}/statement ─────────────────────────────

    [Fact]
    public async Task GET_Extrato_ContaInexistente_DeveRetornar200ComListaVazia()
    {
        var response = await _client.GetAsync(
            $"/api/v1/accounts/{Guid.NewGuid()}/statement");

        // extrato retorna 200 com lista vazia mesmo sem conta
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GET_Extrato_ComFiltros_DeveRetornar200()
    {
        var response = await _client.GetAsync(
            $"/api/v1/accounts/{Guid.NewGuid()}/statement" +
            "?dataInicio=2024-01-01&dataFim=2024-12-31&tipo=Deposito&pagina=1&tamanhoPagina=10");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}