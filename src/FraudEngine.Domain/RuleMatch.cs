namespace FraudEngine.Domain;

/// <summary>
/// Represents a single rule that fired against a transaction context.
/// </summary>
public sealed record RuleMatch(
    string RuleReason,
    int ScoreDelta);
