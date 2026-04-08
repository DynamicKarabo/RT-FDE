namespace FraudEngine.Domain;

/// <summary>
/// Explicit, named constants for all possible fraud signals.
/// No magic strings anywhere.
/// </summary>
public static class RuleReasons
{
    public const string HighAmountAnomaly = "HIGH_AMOUNT_ANOMALY";
    public const string HighVelocity = "HIGH_VELOCITY";
    public const string GeoAnomaly = "GEO_ANOMALY";
    public const string NewDevice = "NEW_DEVICE";
}
