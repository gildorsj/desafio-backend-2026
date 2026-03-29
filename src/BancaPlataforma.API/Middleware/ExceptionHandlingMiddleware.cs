using BancaPlataforma.Domain.Exceptions;
using System.Text.Json;

namespace BancaPlataforma.API.Middleware;

public sealed class ExceptionHandlingMiddleware(
    RequestDelegate next,
    ILogger<ExceptionHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (DomainException ex)
        {
            logger.LogWarning("Domain exception: {Message}", ex.Message);
            await EscreverRespostaAsync(context, StatusCodes.Status422UnprocessableEntity, ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro inesperado");
            await EscreverRespostaAsync(context, StatusCodes.Status500InternalServerError, "Erro interno do servidor.");
        }
    }

    private static Task EscreverRespostaAsync(HttpContext context, int statusCode, string mensagem)
    {
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = statusCode;

        var body = JsonSerializer.Serialize(new { erro = mensagem });
        return context.Response.WriteAsync(body);
    }
}
