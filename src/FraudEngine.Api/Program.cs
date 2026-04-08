using FraudEngine.Application.EvaluateTransaction;
using FraudEngine.Application.Validation;
using FraudEngine.Domain;
using FraudEngine.Domain.Services;
using FraudEngine.Api.Middleware;
using FraudEngine.Infrastructure;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using Serilog.Events;

// ── Serilog — structured JSON logging to Azure Monitor / stdout ──
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .Enrich.WithMachineName()
    .Enrich.WithEnvironmentName()
    .WriteTo.Console(
        outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}")
    .CreateBootstrapLogger();

try
{
    Log.Information("Starting RT-FDE Fraud Engine.");

    var builder = WebApplication.CreateBuilder(args);

    // Replace default logger with Serilog
    builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .Enrich.WithMachineName()
        .WriteTo.Console());

    // ── Configuration ──────────────────────────────────────────────
    builder.Services.Configure<FraudThresholds>(
        builder.Configuration.GetSection("FraudThresholds"));

    builder.Services.Configure<RuleEvaluationThresholds>(
        builder.Configuration.GetSection("RuleEvaluationThresholds"));

    // ── Domain services (pure logic, no infra) ─────────────────────
    builder.Services.AddSingleton<RuleEngine>();
    builder.Services.AddSingleton<RiskScorer>();
    builder.Services.AddSingleton<DecisionEngine>();

    // ── Application ────────────────────────────────────────────────
    builder.Services.AddScoped<IEvaluateTransactionUseCase, EvaluateTransactionUseCase>();

    // ── Validation ─────────────────────────────────────────────────
    builder.Services.AddFluentValidationAutoValidation();
    builder.Services.AddValidatorsFromAssemblyContaining<EvaluateTransactionRequestValidator>();

    // ── Controllers ────────────────────────────────────────────────
    builder.Services.AddControllers();

    // ── Exception handling ─────────────────────────────────────────
    builder.Services.AddExceptionHandler<GlobalExceptionFilter>();
    builder.Services.AddProblemDetails();

    // ── OpenTelemetry — vendor-neutral distributed tracing ─────────
    builder.Services.AddOpenTelemetry()
        .ConfigureResource(r => r.AddService(
            serviceName: "RT-FDE",
            serviceVersion: "v1"))
        .WithTracing(t => t
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation());

    // ── Health checks ──────────────────────────────────────────────
    builder.Services.AddHealthChecks()
        .AddCheck("live", () => HealthCheckResult.Healthy("Process alive"), tags: new[] { "live" })
        .AddCheck("ready", () => HealthCheckResult.Healthy("Dependencies ready"), tags: new[] { "ready" });

    // ── Infrastructure — real implementations (Redis + SQL Server) ─
    // In local/dev environments without Redis/SQL, use stubs via conditional config.
    var useRealInfra = builder.Configuration.GetValue<bool>("UseRealInfrastructure", defaultValue: true);

    if (useRealInfra)
    {
        builder.Services.AddFraudInfrastructure(builder.Configuration);
    }
    else
    {
        // Stub implementations for local development without infra.
        builder.Services.AddSingleton<FraudEngine.Domain.Interfaces.IBehaviourStore, NoOpBehaviourStore>();
        builder.Services.AddSingleton<FraudEngine.Domain.Interfaces.IRuleRepository, NoOpRuleRepository>();
        builder.Services.AddSingleton<FraudEngine.Domain.Interfaces.IFraudDecisionStore, NoOpFraudDecisionStore>();
    }

    var app = builder.Build();

    app.UseSerilogRequestLogging();
    app.UseExceptionHandler(_ => { });
    app.UseHttpsRedirection();

    // Health endpoints for Kubernetes probes
    app.MapHealthChecks("/health/live", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
    {
        Predicate = r => r.Tags.Contains("live")
    });
    app.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
    {
        Predicate = r => r.Tags.Contains("ready")
    });

    app.MapControllers();

    Log.Information("RT-FDE Fraud Engine started on {Urls}.", builder.Configuration["urls"] ?? "default");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "RT-FDE terminated unexpectedly.");
    throw;
}
finally
{
    Log.CloseAndFlush();
}

// ── Stub implementations for dev/test without real infra ──

file sealed class NoOpBehaviourStore : FraudEngine.Domain.Interfaces.IBehaviourStore
{
    public Task<FraudEngine.Domain.BehaviouralContext?> GetBehaviourAsync(Guid userId, Guid transactionId, CancellationToken ct = default)
        => Task.FromResult<FraudEngine.Domain.BehaviouralContext?>(null);
}

file sealed class NoOpRuleRepository : FraudEngine.Domain.Interfaces.IRuleRepository
{
    public Task<IReadOnlyList<FraudEngine.Domain.RuleDefinition>> LoadActiveRulesAsync(CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<FraudEngine.Domain.RuleDefinition>>(Array.Empty<FraudEngine.Domain.RuleDefinition>());
}

file sealed class NoOpFraudDecisionStore : FraudEngine.Domain.Interfaces.IFraudDecisionStore
{
    public Task<FraudEngine.Domain.FraudDecision?> GetExistingDecisionAsync(Guid transactionId, CancellationToken ct = default)
        => Task.FromResult<FraudEngine.Domain.FraudDecision?>(null);

    public Task PersistDecisionAsync(Guid transactionId, FraudEngine.Domain.FraudDecision decision, CancellationToken ct = default)
        => Task.CompletedTask;
}
