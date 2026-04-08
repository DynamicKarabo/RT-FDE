using FraudEngine.Domain;

namespace FraudEngine.Contracts;

/// <summary>
/// Response contract for POST /v1/fraud/evaluate
/// </summary>
public sealed record FraudDecisionResponse(
    string Decision,
    int RiskScore,
    IReadOnlyList<string> Reasons)
{
    public static FraudDecisionResponse FromDomain(FraudDecision decision) => new(
        decision.Outcome.ToString().ToUpperInvariant(),
        decision.RiskScore,
        decision.Reasons);
}
