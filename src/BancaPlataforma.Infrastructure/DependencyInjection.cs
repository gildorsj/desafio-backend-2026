using MassTransit;
using BancaPlataforma.Application.Common.Interfaces;
using BancaPlataforma.Infrastructure.ExternalServices;
using BancaPlataforma.Infrastructure.Idempotency;
using BancaPlataforma.Infrastructure.Persistence;
using BancaPlataforma.Infrastructure.Persistence.Repositories;
using BancaPlataforma.Infrastructure.ReadModel.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using Polly;
using Polly.Extensions.Http;

namespace BancaPlataforma.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // ── PostgreSQL ──────────────────────────────
        services.AddDbContext<BancaDbContext>(opts =>
            opts.UseNpgsql(configuration.GetConnectionString("PostgreSQL")));

        services.AddScoped<IContaRepository, ContaRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // ── MongoDB ─────────────────────────────────
        var mongoConnectionString = configuration.GetConnectionString("MongoDB")!;
        var mongoDatabaseName = configuration["MongoDB:DatabaseName"]!;

        services.AddSingleton<IMongoClient>(_ => new MongoClient(mongoConnectionString));
        services.AddSingleton(sp =>
            sp.GetRequiredService<IMongoClient>().GetDatabase(mongoDatabaseName));

        services.AddScoped<IContaReadRepository, ContaReadRepository>();

        // ── Redis ────────────────────────────────────
        services.AddStackExchangeRedisCache(opts =>
            opts.Configuration = configuration.GetConnectionString("Redis"));

        services.AddScoped<IIdempotencyService, RedisIdempotencyService>();

        // ── ReceitaWS ────────────────────────────────
        services.AddHttpClient<IReceitaWsService, ReceitaWsService>(client =>
        {
            client.BaseAddress = new Uri("https://receitaws.com.br/");
            client.Timeout = TimeSpan.FromSeconds(10);
        })
        .AddPolicyHandler(RetryPolicy())
        .AddPolicyHandler(CircuitBreakerPolicy());

        return services;
    }

    private static IAsyncPolicy<HttpResponseMessage> RetryPolicy() =>
        HttpPolicyExtensions
            .HandleTransientHttpError()
            .WaitAndRetryAsync(3, attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)));

    private static IAsyncPolicy<HttpResponseMessage> CircuitBreakerPolicy() =>
        HttpPolicyExtensions
            .HandleTransientHttpError()
            .CircuitBreakerAsync(5, TimeSpan.FromSeconds(30));
    
    public static IServiceCollection AddMessaging(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddMassTransit(x =>
        {
            x.UsingRabbitMq((ctx, cfg) =>
            {
                cfg.Host(configuration["RabbitMQ:Host"], configuration["RabbitMQ:VirtualHost"], h =>
                {
                    h.Username(configuration["RabbitMQ:Username"]!);
                    h.Password(configuration["RabbitMQ:Password"]!);
                });
            });
        });

        return services;
    }
}