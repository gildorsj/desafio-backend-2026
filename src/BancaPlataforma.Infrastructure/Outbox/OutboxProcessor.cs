using BancaPlataforma.Infrastructure.Persistence;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace BancaPlataforma.Infrastructure.Outbox;

public sealed class OutboxProcessor(
    IServiceScopeFactory scopeFactory,
    ILogger<OutboxProcessor> logger) : BackgroundService
{
    private static readonly TimeSpan Intervalo = TimeSpan.FromSeconds(10);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Intervalo);

        while (!stoppingToken.IsCancellationRequested &&
               await timer.WaitForNextTickAsync(stoppingToken))
        {
            await ProcessarAsync(stoppingToken);
        }
    }

    private async Task ProcessarAsync(CancellationToken ct)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<BancaDbContext>();
        var bus = scope.ServiceProvider.GetRequiredService<IBus>();

        var pendentes = await db.OutboxMessages
            .Where(m => m.ProcessadoEm == null)
            .OrderBy(m => m.CriadoEm)
            .Take(20)
            .ToListAsync(ct);

        if (pendentes.Count == 0) return;

        foreach (var mensagem in pendentes)
        {
            try
            {
                var tipo = Type.GetType(mensagem.Tipo)
                    ?? throw new InvalidOperationException($"Tipo não encontrado: {mensagem.Tipo}");

                var evento = JsonSerializer.Deserialize(mensagem.Payload, tipo)
                    ?? throw new InvalidOperationException($"Falha ao desserializar mensagem {mensagem.Id}");

                await bus.Publish(evento, tipo, ct);

                mensagem.ProcessadoEm = DateTime.UtcNow;
                logger.LogDebug("Mensagem outbox {Id} ({Tipo}) publicada", mensagem.Id, tipo.Name);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Erro ao processar mensagem outbox {Id}", mensagem.Id);
                mensagem.Erro = ex.Message[..Math.Min(ex.Message.Length, 1000)];
                mensagem.ProcessadoEm = DateTime.UtcNow;
            }
        }

        await db.SaveChangesAsync(ct);
    }
}
