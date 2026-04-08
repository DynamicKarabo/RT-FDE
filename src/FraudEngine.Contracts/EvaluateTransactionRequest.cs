namespace FraudEngine.Contracts;

/// <summary>
/// Request contract for POST /v1/fraud/evaluate
/// </summary>
public sealed record EvaluateTransactionRequest(
    Guid TransactionId,
    Guid UserId,
    decimal Amount,
    string Currency,
    DateTimeOffset Timestamp,
    string IpAddress,
    string DeviceId,
    string MerchantId,
    double? Latitude = null,
    double? Longitude = null);
