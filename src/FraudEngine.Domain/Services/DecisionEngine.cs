namespace FraudEngine.Domain.Services;

/// <summary>
/// Maps a risk score to a final decision outcome using configuration-driven thresholds.
/// </summary>
public sealed class DecisionEngine
{
    /// <summary>
    /// Maps the numeric risk score to APPROVE, REVIEW, or REJECT.
    /// Thresholds are read from configuration — never hardcoded.
    /// Timestamp is set synchronously here to prevent audit log microsecond drift.
    /// </summary>
    public FraudDecision MapScore(int riskScore, IReadOnlyList<string> reasons, FraudThresholds thresholds)
    {
        var outcome = ResolveOutcome(riskScore, thresholds);
        var timestamp = DateTimeOffset.UtcNow;

        return new FraudDecision(outcome, riskScore, reasons, timestamp);
    }

    private static DecisionOutcome ResolveOutcome(int riskScore, FraudThresholds thresholds)
    {
        if (riskScore >= thresholds.RejectThreshold)
            return DecisionOutcome.Reject;

        if (riskScore >= thresholds.ReviewThreshold)
            return DecisionOutcome.Review;

        return DecisionOutcome.Approve;
    }
}
