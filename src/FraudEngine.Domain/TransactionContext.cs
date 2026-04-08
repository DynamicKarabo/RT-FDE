namespace FraudEngine.Domain;

/// <summary>
/// Immutable transaction context fed into the rule engine.
/// </summary>
public sealed record TransactionContext(
    Guid TransactionId,
    Guid UserId,
    decimal Amount,
    string Currency,
    DateTimeOffset Timestamp,
    string IpAddress,
    string DeviceId,
    string MerchantId,
    double? LastKnownLatitude = null,
    double? LastKnownLongitude = null);
