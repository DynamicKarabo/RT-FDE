using FraudEngine.Domain;
using FraudEngine.Domain.Interfaces;
using FraudEngine.Infrastructure.Redis;
using FraudEngine.Infrastructure.Sql;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Polly;
using StackExchange.Redis;

namespace FraudEngine.Infrastructure;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddFraudInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // ── Redis ──────────────────────────────────────────────────
        var redisConnectionString = configuration.GetConnectionString("Redis")
            ?? throw new InvalidOperationException("Redis connection string is required.");

        services.AddSingleton<IConnectionMultiplexer>(_ =>
            ConnectionMultiplexer.Connect(redisConnectionString));

        services.Configure<RedisBehaviourOptions>(
            configuration.GetSection("RedisBehaviour"));

        // ── Polly retry policy for Redis: 2 retries, exponential backoff ──
        // Strictly scoped to RedisConnectionException only.
        var redisRetryPolicy = Policy
            .Handle<RedisConnectionException>()
            .WaitAndRetryAsync(
                retryCount: 2,
                sleepDurationProvider: retryAttempt => TimeSpan.FromMilliseconds(Math.Pow(2, retryAttempt) * 100),
                onRetry: (exception, _, retryAttempt, _) =>
                {
                    // Logged inside the RedisBehaviourStore where TransactionId is available.
                });

        services.AddSingleton<IAsyncPolicy>(redisRetryPolicy);
        services.AddSingleton<IBehaviourStore, RedisBehaviourStore>();

        // ── SQL Server ─────────────────────────────────────────────
        var sqlConnectionString = configuration.GetConnectionString("SqlFraudEngine")
            ?? throw new InvalidOperationException("SQL Server connection string is required.");

        services.AddSingleton(new SqlConnectionFactory(sqlConnectionString));
        services.AddSingleton<IRuleRepository, SqlRuleRepository>();
        services.AddSingleton<IFraudDecisionStore, SqlFraudDecisionStore>();

        // ── Fail behaviour (configuration-driven, default CLOSED) ──
        services.Configure<FailBehaviourConfig>(
            configuration.GetSection("FailBehaviour"));

        return services;
    }
}
