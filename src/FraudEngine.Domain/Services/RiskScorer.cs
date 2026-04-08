namespace FraudEngine.Domain.Services;

/// <summary>
/// Aggregates matched rule deltas into a single 0–100 risk score.
/// Pure deterministic sum — no ML, no randomness.
/// </summary>
public sealed class RiskScorer
{
    /// <summary>
    /// Computes the risk score from matched rules.
    /// Score is the sum of all deltas, capped at 100.
    /// </summary>
    public int Compute(IReadOnlyList<RuleMatch> matches)
    {
        if (matches.Count == 0)
            return 0;

        var sum = 0;
        foreach (var match in matches)
        {
            sum += match.ScoreDelta;
        }

        return Math.Min(sum, 100);
    }
}
