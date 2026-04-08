using FraudEngine.Domain;
using FraudEngine.Infrastructure.Redis;
using Microsoft.Extensions.Options;
using Polly;
using StackExchange.Redis;
using Testcontainers.Redis;

namespace FraudEngine.Integration.Redis;

public class RedisBehaviourStoreTests : IAsyncLifetime
{
    private readonly RedisContainer _redisContainer = new RedisBuilder("redis:7-alpine")
        .WithPortBinding(6379, assignRandomHostPort: true)
        .Build();

    private IConnectionMultiplexer _connection = null!;
    private RedisBehaviourStore _store = null!;
    private IDatabase _db = null!;
    private readonly IAsyncPolicy _retryPolicy = Policy
        .Handle<RedisConnectionException>()
        .WaitAndRetryAsync(2, retryAttempt => TimeSpan.FromMilliseconds(Math.Pow(2, retryAttempt) * 100));

    public async Task InitializeAsync()
    {
        await _redisContainer.StartAsync();

        _connection = await ConnectionMultiplexer.ConnectAsync(_redisContainer.GetConnectionString());
        var options = Options.Create(new RedisBehaviourOptions { KeyPrefix = "user" });
        var logger = Microsoft.Extensions.Logging.Abstractions.NullLogger<RedisBehaviourStore>.Instance;
        _store = new RedisBehaviourStore(_connection, logger, options, _retryPolicy);
        _db = _connection.GetDatabase();
    }

    public async Task DisposeAsync()
    {
        if (_connection is not null)
            await _connection.DisposeAsync();
        if (_redisContainer is not null)
            await _redisContainer.DisposeAsync();
    }

    [Fact]
    public async Task GetBehaviourAsync_ReturnsPopulatedContext_WhenDataExists()
    {
        var userId = Guid.NewGuid();
        var keyPrefix = "user";

        // Seed Redis with behavioural data
        await _db.StringSetAsync($"{keyPrefix}:{userId}:txn_count:1min", 7);
        await _db.StringSetAsync($"{keyPrefix}:{userId}:last_ip", "10.0.0.1");
        await _db.StringSetAsync($"{keyPrefix}:{userId}:last_lat", "-26.2041");
        await _db.StringSetAsync($"{keyPrefix}:{userId}:last_lon", "28.0473");
        await _db.StringSetAsync($"{keyPrefix}:{userId}:avg_amount", 1500);
        await _db.SetAddAsync($"{keyPrefix}:{userId}:devices", "device-1");
        await _db.SetAddAsync($"{keyPrefix}:{userId}:devices", "device-2");

        var context = await _store.GetBehaviourAsync(userId, Guid.NewGuid());

        Assert.NotNull(context);
        Assert.Equal(7, context!.TransactionCountLast60s);
        Assert.Equal("10.0.0.1", context.LastKnownIp);
        Assert.Equal(-26.2041, context.LastKnownLatitude);
        Assert.Equal(28.0473, context.LastKnownLongitude);
        Assert.Equal(1500m, context.AverageTransactionAmount);
        Assert.Equal(2, context.KnownDeviceIds.Count);
        Assert.Contains("device-1", context.KnownDeviceIds);
        Assert.Contains("device-2", context.KnownDeviceIds);
    }

    [Fact]
    public async Task GetBehaviourAsync_ReturnsZeroValues_WhenNoDataExists()
    {
        var userId = Guid.NewGuid();

        var context = await _store.GetBehaviourAsync(userId, Guid.NewGuid());

        Assert.NotNull(context);
        Assert.Equal(0, context!.TransactionCountLast60s);
        Assert.Equal(0m, context.AverageTransactionAmount);
        Assert.Empty(context.KnownDeviceIds);
        Assert.Null(context.LastKnownIp);
        Assert.Null(context.LastKnownLatitude);
        Assert.Null(context.LastKnownLongitude);
    }
}
