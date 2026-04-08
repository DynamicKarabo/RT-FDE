namespace FraudEngine.Domain;

/// <summary>
/// Ephemeral behavioural state for a user, sourced from Redis.
/// All values are optional — missing data means degraded-mode evaluation.
/// </summary>
public sealed record BehaviouralContext(
    int TransactionCountLast60s,
    string? LastKnownIp,
    IReadOnlyList<string> KnownDeviceIds,
    double? LastKnownLatitude,
    double? LastKnownLongitude,
    decimal AverageTransactionAmount);
