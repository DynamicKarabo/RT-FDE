namespace FraudEngine.Domain;

/// <summary>
/// The immutable result of a fraud evaluation.
/// Uses the Result pattern — never throws for expected business outcomes.
/// Timestamp is set synchronously at the moment of domain evaluation to prevent audit drift.
/// </summary>
public sealed record FraudDecision(
    DecisionOutcome Outcome,
    int RiskScore,
    IReadOnlyList<string> Reasons,
    DateTimeOffset Timestamp);
