using BancaPlataforma.API.Models;
using BancaPlataforma.Application.Accounts.Commands.AbrirConta;
using BancaPlataforma.Application.Accounts.Commands.AlterarStatusConta;
using BancaPlataforma.Application.Accounts.Commands.Depositar;
using BancaPlataforma.Application.Accounts.Commands.EncerrarConta;
using BancaPlataforma.Application.Accounts.Commands.Sacar;
using BancaPlataforma.Application.Accounts.Commands.Transferir;
using BancaPlataforma.Application.Accounts.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace BancaPlataforma.API.Controllers;

[ApiController]
[Route("api/v1/accounts")]
[Produces("application/json")]
public sealed class AccountsController(IMediator mediator) : ControllerBase
{
    // ── Contas ───────────────────────────────────────────────────

    /// <summary>Abre uma nova conta bancária para uma empresa.</summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> AbrirConta(
        [FromBody] AbrirContaRequest request,
        CancellationToken ct)
    {
        var command = new AbrirContaCommand(request.Cnpj, request.Agencia, request.ImagemDocumento);
        var id = await mediator.Send(command, ct);
        return CreatedAtAction(nameof(ObterPorId), new { id }, new { id });
    }

    /// <summary>Obtém uma conta pelo ID.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ObterPorId(Guid id, CancellationToken ct)
    {
        var conta = await mediator.Send(new ObterContaPorIdQuery(id), ct);
        return conta is null ? NotFound() : Ok(conta);
    }

    /// <summary>Obtém uma conta pelo CNPJ.</summary>
    [HttpGet("cnpj/{cnpj}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ObterPorCnpj(string cnpj, CancellationToken ct)
    {
        var conta = await mediator.Send(new ObterContaPorCnpjQuery(cnpj), ct);
        return conta is null ? NotFound() : Ok(conta);
    }

    /// <summary>Altera o status da conta.</summary>
    [HttpPatch("{id:guid}/status")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> AlterarStatus(
        Guid id,
        [FromBody] AlterarStatusRequest request,
        CancellationToken ct)
    {
        await mediator.Send(new AlterarStatusContaCommand(id, request.Status), ct);
        return NoContent();
    }

    /// <summary>Encerra a conta (soft delete). Saldo deve ser zero.</summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> EncerrarConta(Guid id, CancellationToken ct)
    {
        await mediator.Send(new EncerrarContaCommand(id), ct);
        return NoContent();
    }

    // ── Operações Financeiras ────────────────────────────────────

    /// <summary>Realiza um depósito na conta.</summary>
    [HttpPost("{id:guid}/deposit")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Depositar(
        Guid id,
        [FromBody] OperacaoFinanceiraRequest request,
        CancellationToken ct)
    {
        var command = new DepositarCommand(id, request.IdempotencyKey, request.Valor, request.Moeda, request.Descricao);
        await mediator.Send(command, ct);
        return NoContent();
    }

    /// <summary>Realiza um saque na conta.</summary>
    [HttpPost("{id:guid}/withdraw")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Sacar(
        Guid id,
        [FromBody] OperacaoFinanceiraRequest request,
        CancellationToken ct)
    {
        var command = new SacarCommand(id, request.IdempotencyKey, request.Valor, request.Moeda, request.Descricao);
        await mediator.Send(command, ct);
        return NoContent();
    }

    /// <summary>Realiza uma transferência entre contas.</summary>
    [HttpPost("{id:guid}/transfer")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Transferir(
        Guid id,
        [FromBody] TransferenciaRequest request,
        CancellationToken ct)
    {
        var command = new TransferirCommand(
            id, request.IdempotencyKey, request.ContaDestinoId,
            request.Valor, request.Moeda, request.Descricao);
        await mediator.Send(command, ct);
        return NoContent();
    }

    // ── Consultas (Read Side) ────────────────────────────────────

    /// <summary>Consulta o saldo da conta (Read Model - MongoDB).</summary>
    [HttpGet("{id:guid}/balance")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ObterSaldo(Guid id, CancellationToken ct)
    {
        var saldo = await mediator.Send(new ObterSaldoQuery(id), ct);
        return saldo is null ? NotFound() : Ok(saldo);
    }

    /// <summary>Retorna o extrato da conta com filtros e paginação (Read Model - MongoDB).</summary>
    [HttpGet("{id:guid}/statement")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> ObterExtrato(
        Guid id,
        [FromQuery] string? dataInicio,
        [FromQuery] string? dataFim,
        [FromQuery] string? tipo,
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanhoPagina = 20,
        CancellationToken ct = default)
    {
        DateTime? inicio = dataInicio is not null ? DateTime.Parse(dataInicio) : null;
        DateTime? fim = dataFim is not null ? DateTime.Parse(dataFim) : null;

        var query = new ExtratoQuery(id, inicio, fim, tipo, pagina, tamanhoPagina);
        var extrato = await mediator.Send(query, ct);
        return Ok(extrato);
    }
}