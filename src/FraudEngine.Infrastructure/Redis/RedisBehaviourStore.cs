using FraudEngine.Domain;
using FraudEngine.Domain.Errors;
using FraudEngine.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using StackExchange.Redis;

namespace FraudEngine.Infrastructure.Redis;

/// <summary>
/// Redis-backed implementation of IBehaviourStore.
/// Keys carry TTLs; Redis is treated as a cache — absence must not block evaluation.
/// Redis operations are wrapped in a Polly retry policy (2 retries, exponential backoff)
/// scoped strictly to RedisConnectionException.
/// </summary>
public sealed class RedisBehaviourStore : IBehaviourStore
{
    private readonly IConnectionMultiplexer _connection;
    private readonly ILogger<RedisBehaviourStore> _logger;
    private readonly RedisBehaviourOptions _options;
    private readonly IAsyncPolicy _retryPolicy;

    public RedisBehaviourStore(
        IConnectionMultiplexer connection,
        ILogger<RedisBehaviourStore> logger,
        IOptions<RedisBehaviourOptions> options,
        IAsyncPolicy retryPolicy)
    {
        _connection = connection;
        _logger = logger;
        _options = options.Value;
        _retryPolicy = retryPolicy;
    }

    public async Task<BehaviouralContext?> GetBehaviourAsync(Guid userId, Guid transactionId, CancellationToken ct = default)
    {
        var db = _connection.GetDatabase();

        try
        {
            return await _retryPolicy.ExecuteAsync(async () =>
            {
                var txnCountKey = _options.BuildTxnCountKey(userId);
                var devicesKey = _options.BuildDevicesKey(userId);
                var lastIpKey = _options.BuildLastIpKey(userId);
                var lastLatKey = _options.BuildLastLatKey(userId);
                var lastLonKey = _options.BuildLastLonKey(userId);
                var avgAmountKey = _options.BuildAvgAmountKey(userId);

                // Fire all reads in parallel — Redis is single-threaded per connection but multiplexed
                var txnCountTask = db.StringGetAsync(txnCountKey);
                var devicesTask = db.SetMembersAsync(devicesKey);
                var lastIpTask = db.StringGetAsync(lastIpKey);
                var lastLatTask = db.StringGetAsync(lastLatKey);
                var lastLonTask = db.StringGetAsync(lastLonKey);
                var avgAmountTask = db.StringGetAsync(avgAmountKey);

                await Task.WhenAll(txnCountTask, devicesTask, lastIpTask, lastLatTask, lastLonTask, avgAmountTask);

                var txnCountResult = await txnCountTask;
                var devicesResult = await devicesTask;
                var lastIpResult = await lastIpTask;
                var lastLatResult = await lastLatTask;
                var lastLonResult = await lastLonTask;
                var avgAmountResult = await avgAmountTask;

                var txnCount = txnCountResult.HasValue ? (int)txnCountResult : 0;
                var knownDevices = devicesResult.Select(v => v.ToString()).ToList();
                var lastIp = lastIpResult.HasValue ? (string?)lastIpResult : null;
                var lastLat = lastLatResult.HasValue ? (double)lastLatResult : (double?)null;
                var lastLon = lastLonResult.HasValue ? (double)lastLonResult : (double?)null;
                var avgAmount = avgAmountResult.HasValue ? (decimal)avgAmountResult : 0m;

                return new BehaviouralContext(
                    TransactionCountLast60s: txnCount,
                    LastKnownIp: lastIp,
                    KnownDeviceIds: knownDevices,
                    LastKnownLatitude: lastLat,
                    LastKnownLongitude: lastLon,
                    AverageTransactionAmount: avgAmount);
            });
        }
        catch (RedisConnectionException ex)
        {
            _logger.LogWarning(
                ex,
                "Redis connection failure for user {UserId}. TransactionId: {TransactionId}. Polly retries exhausted. Proceeding with degraded scoring.",
                userId, transactionId);
            throw new BehaviourStoreUnavailableException("Redis connection unavailable after retries.", ex);
        }
        catch (RedisTimeoutException ex)
        {
            _logger.LogWarning(
                ex,
                "Redis timeout for user {UserId}. TransactionId: {TransactionId}. Proceeding with degraded scoring.",
                userId, transactionId);
            throw new BehaviourStoreUnavailableException("Redis operation timed out.", ex);
        }
    }
}
