using BancaPlataforma.Domain.Events;
using BancaPlataforma.Worker;
using BancaPlataforma.Worker.Consumers;
using MassTransit;
using MongoDB.Driver;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

var host = Host.CreateDefaultBuilder(args)
    .UseSerilog((ctx, cfg) =>
        cfg.ReadFrom.Configuration(ctx.Configuration)
           .WriteTo.Console())
    .ConfigureServices((ctx, services) =>
    {
        var config = ctx.Configuration;

        // ── MongoDB ──────────────────────────────────────────────
        var mongoConnectionString = config.GetConnectionString("MongoDB")!;
        var mongoDatabaseName = config["MongoDB:DatabaseName"]!;

        services.AddSingleton<IMongoClient>(_ => new MongoClient(mongoConnectionString));
        services.AddSingleton(sp =>
            sp.GetRequiredService<IMongoClient>().GetDatabase(mongoDatabaseName));

        // ── MassTransit + RabbitMQ ───────────────────────────────
        services.AddMassTransit(x =>
        {
            x.AddConsumer<ContaAbertaConsumer>();
            x.AddConsumer<DepositoRealizadoConsumer>();
            x.AddConsumer<SaqueRealizadoConsumer>();
            x.AddConsumer<TransferenciaRealizadaConsumer>();
            x.AddConsumer<StatusContaAlteradoConsumer>();

            x.UsingRabbitMq((ctx, cfg) =>
            {
                cfg.Host(config["RabbitMQ:Host"], config["RabbitMQ:VirtualHost"], h =>
                {
                    h.Username(config["RabbitMQ:Username"]!);
                    h.Password(config["RabbitMQ:Password"]!);
                });

                // Uma fila por tipo de evento
                cfg.ReceiveEndpoint("conta-aberta", e =>
                {
                    e.ConfigureConsumer<ContaAbertaConsumer>(ctx);
                    e.UseMessageRetry(r => r.Intervals(500, 1000, 2000));
                });

                cfg.ReceiveEndpoint("deposito-realizado", e =>
                {
                    e.ConfigureConsumer<DepositoRealizadoConsumer>(ctx);
                    e.UseMessageRetry(r => r.Intervals(500, 1000, 2000));
                });

                cfg.ReceiveEndpoint("saque-realizado", e =>
                {
                    e.ConfigureConsumer<SaqueRealizadoConsumer>(ctx);
                    e.UseMessageRetry(r => r.Intervals(500, 1000, 2000));
                });

                cfg.ReceiveEndpoint("transferencia-realizada", e =>
                {
                    e.ConfigureConsumer<TransferenciaRealizadaConsumer>(ctx);
                    e.UseMessageRetry(r => r.Intervals(500, 1000, 2000));
                });

                cfg.ReceiveEndpoint("status-conta-alterado", e =>
                {
                    e.ConfigureConsumer<StatusContaAlteradoConsumer>(ctx);
                    e.UseMessageRetry(r => r.Intervals(500, 1000, 2000));
                });
            });
        });
    })
    .Build();

using (var scope = host.Services.CreateScope())
{
    var database = scope.ServiceProvider.GetRequiredService<IMongoDatabase>();
    await MongoDbIndexes.CriarAsync(database);
}

await host.RunAsync();