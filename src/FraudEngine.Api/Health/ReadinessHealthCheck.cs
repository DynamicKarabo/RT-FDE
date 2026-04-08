using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using StackExchange.Redis;
using FraudEngine.Infrastructure.Sql;

namespace FraudEngine.Api.Health;

/// <summary>
/// Readiness health check — probes real Redis and SQL Server.
/// Only registered when UseRealInfrastructure=true.
/// </summary>
public sealed class ReadinessHealthCheck : IHealthCheck
{
    private readonly IConnectionMultiplexer _redis;
    private readonly SqlConnectionFactory _sqlFactory;

    public ReadinessHealthCheck(IConnectionMultiplexer redis, SqlConnectionFactory sqlFactory)
    {
        _redis = redis;
        _sqlFactory = sqlFactory;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken ct = default)
    {
        var data = new Dictionary<string, object>();
        var allHealthy = true;

        // Probe Redis
        try
        {
            var db = _redis.GetDatabase();
            await db.PingAsync();
            data["Redis"] = "OK";
        }
        catch (Exception ex)
        {
            data["Redis"] = ex.Message;
            allHealthy = false;
        }

        // Probe SQL Server
        try
        {
            await using var connection = _sqlFactory.CreateConnection();
            await connection.OpenAsync(ct);
            data["SqlServer"] = "OK";
        }
        catch (Exception ex)
        {
            data["SqlServer"] = ex.Message;
            allHealthy = false;
        }

        return allHealthy
            ? HealthCheckResult.Healthy("All dependencies reachable.", data)
            : HealthCheckResult.Unhealthy("One or more dependencies unreachable.", data: data);
    }
}
