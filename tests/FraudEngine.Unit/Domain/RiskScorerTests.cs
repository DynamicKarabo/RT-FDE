using FraudEngine.Domain;
using FraudEngine.Domain.Services;

namespace FraudEngine.Unit.Domain;

public class RiskScorerTests
{
    private readonly RiskScorer _sut = new();

    [Fact]
    public void Compute_ReturnsZero_WhenNoMatches()
    {
        Assert.Equal(0, _sut.Compute(Array.Empty<RuleMatch>()));
    }

    [Fact]
    public void Compute_SumsDeltas()
    {
        var matches = new List<RuleMatch>
        {
            new(RuleReasons.HighAmountAnomaly, 30),
            new(RuleReasons.HighVelocity, 25),
            new(RuleReasons.NewDevice, 20),
        };

        Assert.Equal(75, _sut.Compute(matches));
    }

    [Fact]
    public void Compute_CapsAt100()
    {
        var matches = new List<RuleMatch>
        {
            new(RuleReasons.HighAmountAnomaly, 30),
            new(RuleReasons.HighVelocity, 25),
            new(RuleReasons.GeoAnomaly, 25),
            new(RuleReasons.NewDevice, 20),
            new(RuleReasons.HighAmountAnomaly, 30), // duplicate — sum exceeds 100
        };

        Assert.Equal(100, _sut.Compute(matches));
    }

    [Fact]
    public void Compute_ReturnsSingleRuleDelta()
    {
        var matches = new List<RuleMatch> { new(RuleReasons.NewDevice, 20) };

        Assert.Equal(20, _sut.Compute(matches));
    }
}
