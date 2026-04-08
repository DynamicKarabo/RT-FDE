using FraudEngine.Domain;
using FraudEngine.Domain.Services;

namespace FraudEngine.Unit.Domain;

public class RuleEngineTests
{
    private readonly RuleEngine _sut = new();
    private readonly RuleEvaluationThresholds _thresholds = DefaultThresholds();

    private static TransactionContext DefaultTransaction() => new(
        TransactionId: Guid.NewGuid(),
        UserId: Guid.NewGuid(),
        Amount: 2500m,
        Currency: "ZAR",
        Timestamp: DateTimeOffset.UtcNow,
        IpAddress: "192.168.1.1",
        DeviceId: "device-1",
        MerchantId: "merchant-1");

    private static BehaviouralContext DefaultBehaviour() => new(
        TransactionCountLast60s: 2,
        LastKnownIp: "192.168.1.1",
        KnownDeviceIds: new[] { "device-1" },
        LastKnownLatitude: -26.2041,
        LastKnownLongitude: 28.0473,
        AverageTransactionAmount: 1000m);

    private static RuleEvaluationThresholds DefaultThresholds() => new(
        AmountAnomalyMultiplier: 3.0,
        HighAmountAbsoluteThreshold: 50000m,
        MaxTransactionsPer60s: 5,
        MaxGeoDistanceKm: 1000);

    // ── High Amount Anomaly ──────────────────────────────────────

    [Fact]
    public void HighAmountAnomaly_Matches_WhenAmountExceedsMultiplier()
    {
        var txn = DefaultTransaction() with { Amount = 3500m }; // 3.5x avg of 1000
        var behaviour = DefaultBehaviour();
        var rules = new[] { new RuleDefinition(Guid.NewGuid(), "Amount anomaly", RuleType.HighAmountAnomaly, 30, true) };

        var matches = _sut.Evaluate(rules, txn, behaviour, _thresholds);

        Assert.Contains(matches, m => m.RuleReason == RuleReasons.HighAmountAnomaly);
    }

    [Fact]
    public void HighAmountAnomaly_DoesNotMatch_WhenAmountBelowMultiplier()
    {
        var txn = DefaultTransaction() with { Amount = 2500m }; // 2.5x avg of 1000
        var behaviour = DefaultBehaviour();
        var rules = new[] { new RuleDefinition(Guid.NewGuid(), "Amount anomaly", RuleType.HighAmountAnomaly, 30, true) };

        var matches = _sut.Evaluate(rules, txn, behaviour, _thresholds);

        Assert.DoesNotContain(matches, m => m.RuleReason == RuleReasons.HighAmountAnomaly);
    }

    [Fact]
    public void HighAmountAnomaly_Matches_WhenNoBehaviourAndAmountExceedsAbsolute()
    {
        var txn = DefaultTransaction() with { Amount = 60000m }; // above 50000 absolute threshold
        var rules = new[] { new RuleDefinition(Guid.NewGuid(), "Amount anomaly", RuleType.HighAmountAnomaly, 30, true) };

        var matches = _sut.Evaluate(rules, txn, null, _thresholds);

        Assert.Contains(matches, m => m.RuleReason == RuleReasons.HighAmountAnomaly);
    }

    [Fact]
    public void HighAmountAnomaly_DoesNotMatch_WhenNoBehaviourAndAmountBelowAbsolute()
    {
        var txn = DefaultTransaction() with { Amount = 2500m }; // below 50000 absolute threshold
        var rules = new[] { new RuleDefinition(Guid.NewGuid(), "Amount anomaly", RuleType.HighAmountAnomaly, 30, true) };

        var matches = _sut.Evaluate(rules, txn, null, _thresholds);

        Assert.DoesNotContain(matches, m => m.RuleReason == RuleReasons.HighAmountAnomaly);
    }

    // ── High Velocity ───────────────────────────────────────────

    [Fact]
    public void HighVelocity_Matches_WhenTxnCountExceedsThreshold()
    {
        var behaviour = DefaultBehaviour() with { TransactionCountLast60s = 7 }; // above 5
        var rules = new[] { new RuleDefinition(Guid.NewGuid(), "Velocity", RuleType.HighVelocity, 25, true) };

        var matches = _sut.Evaluate(rules, DefaultTransaction(), behaviour, _thresholds);

        Assert.Contains(matches, m => m.RuleReason == RuleReasons.HighVelocity);
    }

    [Fact]
    public void HighVelocity_DoesNotMatch_WhenTxnCountBelowThreshold()
    {
        var behaviour = DefaultBehaviour() with { TransactionCountLast60s = 3 }; // below 5
        var rules = new[] { new RuleDefinition(Guid.NewGuid(), "Velocity", RuleType.HighVelocity, 25, true) };

        var matches = _sut.Evaluate(rules, DefaultTransaction(), behaviour, _thresholds);

        Assert.DoesNotContain(matches, m => m.RuleReason == RuleReasons.HighVelocity);
    }

    [Fact]
    public void HighVelocity_DoesNotMatch_WhenNoBehaviour()
    {
        var rules = new[] { new RuleDefinition(Guid.NewGuid(), "Velocity", RuleType.HighVelocity, 25, true) };

        var matches = _sut.Evaluate(rules, DefaultTransaction(), null, _thresholds);

        Assert.DoesNotContain(matches, m => m.RuleReason == RuleReasons.HighVelocity);
    }

    // ── Geo Anomaly ─────────────────────────────────────────────

    [Fact]
    public void GeoAnomaly_Matches_WhenDistanceExceedsThreshold()
    {
        var behaviour = DefaultBehaviour() with
        {
            LastKnownLatitude = -26.2041,  // Johannesburg
            LastKnownLongitude = 28.0473
        };
        var txn = DefaultTransaction() with
        {
            LastKnownLatitude = 51.5074,   // London — way more than 1000km away
            LastKnownLongitude = -0.1278
        };
        var rules = new[] { new RuleDefinition(Guid.NewGuid(), "Geo", RuleType.GeoAnomaly, 25, true) };

        var matches = _sut.Evaluate(rules, txn, behaviour, _thresholds);

        Assert.Contains(matches, m => m.RuleReason == RuleReasons.GeoAnomaly);
    }

    [Fact]
    public void GeoAnomaly_DoesNotMatch_WhenDistanceBelowThreshold()
    {
        var behaviour = DefaultBehaviour() with
        {
            LastKnownLatitude = -26.2041,
            LastKnownLongitude = 28.0473
        };
        var txn = DefaultTransaction() with
        {
            LastKnownLatitude = -26.1,     // very close — ~12km
            LastKnownLongitude = 28.1
        };
        var rules = new[] { new RuleDefinition(Guid.NewGuid(), "Geo", RuleType.GeoAnomaly, 25, true) };

        var matches = _sut.Evaluate(rules, txn, behaviour, _thresholds);

        Assert.DoesNotContain(matches, m => m.RuleReason == RuleReasons.GeoAnomaly);
    }

    [Fact]
    public void GeoAnomaly_DoesNotMatch_WhenNoBehaviourCoordinates()
    {
        var behaviour = DefaultBehaviour() with { LastKnownLatitude = null, LastKnownLongitude = null };
        var rules = new[] { new RuleDefinition(Guid.NewGuid(), "Geo", RuleType.GeoAnomaly, 25, true) };

        var matches = _sut.Evaluate(rules, DefaultTransaction(), behaviour, _thresholds);

        Assert.DoesNotContain(matches, m => m.RuleReason == RuleReasons.GeoAnomaly);
    }

    // ── New Device ──────────────────────────────────────────────

    [Fact]
    public void NewDevice_Matches_WhenDeviceNotKnown()
    {
        var behaviour = DefaultBehaviour() with { KnownDeviceIds = new[] { "device-99" } };
        var rules = new[] { new RuleDefinition(Guid.NewGuid(), "Device", RuleType.NewDevice, 20, true) };

        var matches = _sut.Evaluate(rules, DefaultTransaction(), behaviour, _thresholds);

        Assert.Contains(matches, m => m.RuleReason == RuleReasons.NewDevice);
    }

    [Fact]
    public void NewDevice_DoesNotMatch_WhenDeviceIsKnown()
    {
        var behaviour = DefaultBehaviour() with { KnownDeviceIds = new[] { "device-1" } };
        var rules = new[] { new RuleDefinition(Guid.NewGuid(), "Device", RuleType.NewDevice, 20, true) };

        var matches = _sut.Evaluate(rules, DefaultTransaction(), behaviour, _thresholds);

        Assert.DoesNotContain(matches, m => m.RuleReason == RuleReasons.NewDevice);
    }

    [Fact]
    public void NewDevice_Matches_WhenNoBehaviourData()
    {
        var rules = new[] { new RuleDefinition(Guid.NewGuid(), "Device", RuleType.NewDevice, 20, true) };

        var matches = _sut.Evaluate(rules, DefaultTransaction(), null, _thresholds);

        Assert.Contains(matches, m => m.RuleReason == RuleReasons.NewDevice);
    }

    // ── Inactive rules ──────────────────────────────────────────

    [Fact]
    public void InactiveRules_AreSkipped()
    {
        var rules = new[] { new RuleDefinition(Guid.NewGuid(), "Device", RuleType.NewDevice, 20, false) };

        var matches = _sut.Evaluate(rules, DefaultTransaction(), null, _thresholds);

        Assert.Empty(matches);
    }

    // ── Additive scoring ────────────────────────────────────────

    [Fact]
    public void MultipleRules_AllMatchingRulesFire()
    {
        var behaviour = DefaultBehaviour() with
        {
            TransactionCountLast60s = 7,
            KnownDeviceIds = new[] { "device-99" }
        };
        var txn = DefaultTransaction() with { Amount = 5000m }; // 5x avg
        var rules = new List<RuleDefinition>
        {
            new(Guid.NewGuid(), "Amount", RuleType.HighAmountAnomaly, 30, true),
            new(Guid.NewGuid(), "Velocity", RuleType.HighVelocity, 25, true),
            new(Guid.NewGuid(), "Device", RuleType.NewDevice, 20, true),
        };

        var matches = _sut.Evaluate(rules, txn, behaviour, _thresholds);

        Assert.Equal(3, matches.Count);
    }
}
