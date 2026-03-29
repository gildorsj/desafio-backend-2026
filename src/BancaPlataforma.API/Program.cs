using BancaPlataforma.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using BancaPlataforma.API.Middleware;
using BancaPlataforma.Application.Accounts.Commands.AbrirConta;
using BancaPlataforma.Infrastructure;
using FluentValidation;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    // ── Serilog ──────────────────────────────────────────────────
    builder.Host.UseSerilog((ctx, cfg) =>
        cfg.ReadFrom.Configuration(ctx.Configuration)
           .WriteTo.Console());

    // ── MediatR ──────────────────────────────────────────────────
    builder.Services.AddMediatR(cfg =>
        cfg.RegisterServicesFromAssembly(
            typeof(AbrirContaCommand).Assembly));

    // ── FluentValidation ─────────────────────────────────────────
    builder.Services.AddValidatorsFromAssembly(
        typeof(AbrirContaCommandValidator).Assembly);

    // ── Infrastructure ───────────────────────────────────────────
    builder.Services.AddInfrastructure(builder.Configuration);
    builder.Services.AddMessaging(builder.Configuration);

    // ── Controllers + Swagger ────────────────────────────────────
    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(opts =>
    {
        opts.SwaggerDoc("v1", new()
        {
            Title = "Plataforma Bancária API",
            Version = "v1",
            Description = "API de contas bancárias para empresas (CNPJ)"
        });

        var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
        var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
        if (File.Exists(xmlPath))
            opts.IncludeXmlComments(xmlPath);
    });

    // ── HealthChecks ─────────────────────────────────────────────
    builder.Services.AddHealthChecks()
        .AddNpgSql(builder.Configuration.GetConnectionString("PostgreSQL")!)
        .AddMongoDb(sp => sp.GetRequiredService<MongoDB.Driver.IMongoClient>())
        .AddRedis(builder.Configuration.GetConnectionString("Redis")!);

    var app = builder.Build();

    // ── Middleware pipeline ───────────────────────────────────────
    app.UseMiddleware<ExceptionHandlingMiddleware>();

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI(opts =>
        {
            opts.SwaggerEndpoint("/swagger/v1/swagger.json", "Plataforma Bancária v1");
            opts.RoutePrefix = string.Empty; // Swagger na raiz: http://localhost:8080
        });
    }

    app.MapControllers();
    app.MapHealthChecks("/health");

    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<BancaDbContext>();
        await db.Database.MigrateAsync();
    }

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Aplicação encerrou inesperadamente");
}
finally
{
    Log.CloseAndFlush();
}

public partial class Program { }