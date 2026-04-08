namespace FraudEngine.Domain;

/// <summary>
/// Strongly-typed fraud rule categories.
/// Adding a new rule type is a compile-time change — no magic strings.
/// </summary>
public enum RuleType
{
    HighAmountAnomaly = 1,
    HighVelocity = 2,
    GeoAnomaly = 3,
    NewDevice = 4,
}
