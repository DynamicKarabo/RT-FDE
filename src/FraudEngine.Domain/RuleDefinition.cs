namespace FraudEngine.Domain;

/// <summary>
/// A single fraud rule loaded from the RuleDefinitions table.
/// Rules are additive — each carries a score delta when it matches.
/// </summary>
public sealed record RuleDefinition(
    Guid RuleId,
    string RuleName,
    string RuleReason,
    int ScoreDelta,
    bool IsActive);
