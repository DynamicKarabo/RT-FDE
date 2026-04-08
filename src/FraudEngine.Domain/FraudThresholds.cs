namespace FraudEngine.Domain;

/// <summary>
/// Decision thresholds — configuration-driven, never hardcoded.
/// </summary>
public sealed record FraudThresholds(int ReviewThreshold, int RejectThreshold);
