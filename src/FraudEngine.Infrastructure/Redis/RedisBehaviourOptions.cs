namespace FraudEngine.Infrastructure.Redis;

/// <summary>
/// Configuration for Redis behaviour store key patterns.
/// Key format: noun:{id}:descriptor:window (per coding standards).
/// </summary>
public sealed record RedisBehaviourOptions
{
    public string KeyPrefix { get; init; } = "user";

    public string BuildTxnCountKey(Guid userId) => $"{KeyPrefix}:{userId}:txn_count:1min";
    public string BuildDevicesKey(Guid userId) => $"{KeyPrefix}:{userId}:devices";
    public string BuildLastIpKey(Guid userId) => $"{KeyPrefix}:{userId}:last_ip";
    public string BuildLastLatKey(Guid userId) => $"{KeyPrefix}:{userId}:last_lat";
    public string BuildLastLonKey(Guid userId) => $"{KeyPrefix}:{userId}:last_lon";
    public string BuildAvgAmountKey(Guid userId) => $"{KeyPrefix}:{userId}:avg_amount";
}
