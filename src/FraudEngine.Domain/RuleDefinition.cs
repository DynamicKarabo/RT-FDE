namespace FraudEngine.Domain;

/// <summary>
/// A single fraud rule loaded from the RuleDefinitions table.
/// Rules are additive — each carries a score delta when it matches.
/// </summary>
public sealed record RuleDefinition(
    Guid RuleId,
    string RuleName,
    RuleType RuleType,
    int ScoreDelta,
    bool IsActive)
{
    /// <summary>
    /// Derives the human-readable reason string from the rule type.
    /// </summary>
    public string RuleReason => RuleType switch
    {
        RuleType.HighAmountAnomaly => RuleReasons.HighAmountAnomaly,
        RuleType.HighVelocity => RuleReasons.HighVelocity,
        RuleType.GeoAnomaly => RuleReasons.GeoAnomaly,
        RuleType.NewDevice => RuleReasons.NewDevice,
        _ => throw new InvalidOperationException($"Unknown rule type: {RuleType}")
    };
}
