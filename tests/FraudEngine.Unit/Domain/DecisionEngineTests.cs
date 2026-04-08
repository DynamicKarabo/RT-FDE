using FraudEngine.Domain;
using FraudEngine.Domain.Services;

namespace FraudEngine.Unit.Domain;

public class DecisionEngineTests
{
    private readonly DecisionEngine _sut = new();

    [Theory]
    [InlineData(0, DecisionOutcome.Approve)]
    [InlineData(39, DecisionOutcome.Approve)]
    public void MapScore_ReturnsApprove_WhenScoreBelowReviewThreshold(int score, DecisionOutcome expected)
    {
        var thresholds = new FraudThresholds(40, 70);
        var decision = _sut.MapScore(score, Array.Empty<string>(), thresholds);

        Assert.Equal(expected, decision.Outcome);
    }

    [Theory]
    [InlineData(40, DecisionOutcome.Review)]
    [InlineData(69, DecisionOutcome.Review)]
    public void MapScore_ReturnsReview_WhenScoreBetweenThresholds(int score, DecisionOutcome expected)
    {
        var thresholds = new FraudThresholds(40, 70);
        var decision = _sut.MapScore(score, Array.Empty<string>(), thresholds);

        Assert.Equal(expected, decision.Outcome);
    }

    [Theory]
    [InlineData(70, DecisionOutcome.Reject)]
    [InlineData(100, DecisionOutcome.Reject)]
    public void MapScore_ReturnsReject_WhenScoreAtOrAboveRejectThreshold(int score, DecisionOutcome expected)
    {
        var thresholds = new FraudThresholds(40, 70);
        var decision = _sut.MapScore(score, Array.Empty<string>(), thresholds);

        Assert.Equal(expected, decision.Outcome);
    }

    [Fact]
    public void MapScore_PreservesReasons()
    {
        var reasons = new[] { "HIGH_AMOUNT_ANOMALY", "NEW_DEVICE" };
        var thresholds = new FraudThresholds(40, 70);

        var decision = _sut.MapScore(55, reasons, thresholds);

        Assert.Equal(reasons, decision.Reasons);
    }

    [Fact]
    public void MapScore_PreservesRiskScore()
    {
        var thresholds = new FraudThresholds(40, 70);

        var decision = _sut.MapScore(87, Array.Empty<string>(), thresholds);

        Assert.Equal(87, decision.RiskScore);
    }

    [Fact]
    public void MapScore_UsesConfigurableThresholds_NotHardcoded()
    {
        // Different thresholds → different decision boundaries
        var thresholds = new FraudThresholds(30, 50);

        // Score 35: above review (30) but below reject (50) → Review
        var decision = _sut.MapScore(35, Array.Empty<string>(), thresholds);
        Assert.Equal(DecisionOutcome.Review, decision.Outcome);

        // Score 50: at reject threshold → Reject
        decision = _sut.MapScore(50, Array.Empty<string>(), thresholds);
        Assert.Equal(DecisionOutcome.Reject, decision.Outcome);
    }
}
