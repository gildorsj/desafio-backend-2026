using Testcontainers.Redis;
using BancaPlataforma.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;
using Testcontainers.MongoDb;
using Testcontainers.RabbitMq;

namespace BancaPlataforma.IntegrationTests;

public sealed class BancaPlataformaWebFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .Build();

    private readonly MongoDbContainer _mongo = new MongoDbBuilder()
        .WithImage("mongo:7")
        .Build();

    private readonly RabbitMqContainer _rabbitMq = new RabbitMqBuilder()
        .WithImage("rabbitmq:3.13-management-alpine")
        .Build();
    
    private readonly RedisContainer _redis = new RedisBuilder()
        .WithImage("redis:7-alpine")
        .Build();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Remove DbContext original
            var dbDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<BancaDbContext>));
            if (dbDescriptor != null) services.Remove(dbDescriptor);

            services.AddDbContext<BancaDbContext>(opts =>
                opts.UseNpgsql(_postgres.GetConnectionString()));

            // Remove Redis original e substitui sem senha
            var redisDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(Microsoft.Extensions.Caching.StackExchangeRedis.RedisCacheOptions));
            if (redisDescriptor != null) services.Remove(redisDescriptor);

            services.AddStackExchangeRedisCache(opts =>
                opts.Configuration = $"{_redis.Hostname}:{_redis.GetMappedPublicPort(6379)}");
        });

        builder.UseSetting("ConnectionStrings:PostgreSQL", _postgres.GetConnectionString());
        builder.UseSetting("ConnectionStrings:MongoDB", _mongo.GetConnectionString());
        builder.UseSetting("MongoDB:DatabaseName", "bancaplataforma_test");
        builder.UseSetting("RabbitMQ:Host", _rabbitMq.Hostname);
        builder.UseSetting("RabbitMQ:VirtualHost", "/");
        builder.UseSetting("RabbitMQ:Username", "guest");
        builder.UseSetting("RabbitMQ:Password", "guest");
    }

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();
        await _mongo.StartAsync();
        await _rabbitMq.StartAsync();
        await _redis.StartAsync();
    }

    public new async Task DisposeAsync()
    {
        await _postgres.DisposeAsync();
        await _mongo.DisposeAsync();
        await _rabbitMq.DisposeAsync();
        await _redis.DisposeAsync();
    }
}