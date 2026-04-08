using FraudEngine.Application.EvaluateTransaction;
using FraudEngine.Contracts;
using FraudEngine.Domain;
using FraudEngine.Domain.Errors;
using FraudEngine.Domain.Interfaces;
using FraudEngine.Domain.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace FraudEngine.Unit.Application;

public class EvaluateTransactionUseCaseTests
{
    private readonly Mock<IFraudDecisionStore> _decisionStoreMock = new();
    private readonly Mock<IBehaviourStore> _behaviourStoreMock = new();
    private readonly Mock<IRuleRepository> _ruleRepositoryMock = new();
    private readonly RuleEngine _ruleEngine = new();
    private readonly RiskScorer _riskScorer = new();
    private readonly DecisionEngine _decisionEngine = new();
    private readonly Mock<ILogger<EvaluateTransactionUseCase>> _loggerMock = new();

    private EvaluateTransactionUseCase CreateSut(
        FraudThresholds? thresholds = null,
        RuleEvaluationThresholds? ruleThresholds = null,
        FailBehaviourConfig? failBehaviour = null)
    {
        var t = thresholds ?? new FraudThresholds(40, 70);
        var rt = ruleThresholds ?? new RuleEvaluationThresholds(3.0, 50000m, 5, 1000);
        var fb = failBehaviour ?? new FailBehaviourConfig(FailOpen: false);

        return new EvaluateTransactionUseCase(
            _decisionStoreMock.Object,
            _behaviourStoreMock.Object,
            _ruleRepositoryMock.Object,
            _ruleEngine,
            _riskScorer,
            _decisionEngine,
            Options.Create(t),
            Options.Create(rt),
            Options.Create(fb),
            _loggerMock.Object);
    }

    private static EvaluateTransactionRequest DefaultRequest() => new(
        TransactionId: Guid.NewGuid(),
        UserId: Guid.NewGuid(),
        Amount: 2500m,
        Currency: "ZAR",
        Timestamp: DateTimeOffset.UtcNow,
        IpAddress: "192.168.1.1",
        DeviceId: "device-1",
        MerchantId: "merchant-1");

    [Fact]
    public async Task ExecuteAsync_ReturnsCachedDecision_WhenTransactionIdAlreadyHasDecision()
    {
        var request = DefaultRequest();
        var cached = new FraudDecision(DecisionOutcome.Approve, 15, Array.Empty<string>(), DateTimeOffset.UtcNow);
        _decisionStoreMock.Setup(x => x.GetExistingDecisionAsync(request.TransactionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(cached);

        var sut = CreateSut();
        var result = await sut.ExecuteAsync(request);

        Assert.True(result.IsSuccess);
        Assert.Same(cached, result.Value);
        _decisionStoreMock.Verify(x => x.PersistDecisionAsync(It.IsAny<Guid>(), It.IsAny<FraudDecision>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_DegradesGracefully_WhenBehaviourStoreThrows()
    {
        var request = DefaultRequest();
        _decisionStoreMock.Setup(x => x.GetExistingDecisionAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((FraudDecision?)null);
        _behaviourStoreMock.Setup(x => x.GetBehaviourAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new BehaviourStoreUnavailableException("Redis down"));
        _ruleRepositoryMock.Setup(x => x.LoadActiveRulesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<RuleDefinition>());

        var sut = CreateSut();
        var result = await sut.ExecuteAsync(request);

        Assert.True(result.IsSuccess);
        // Should still produce a decision (with no behavioural signals)
        Assert.Equal(DecisionOutcome.Approve, result.Value.Outcome);
        _decisionStoreMock.Verify(x => x.PersistDecisionAsync(request.TransactionId, result.Value, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_EvaluatesRules_WhenNoCachedDecision()
    {
        var request = DefaultRequest();
        var behaviour = new BehaviouralContext(
            TransactionCountLast60s: 7,
            LastKnownIp: "192.168.1.1",
            KnownDeviceIds: new[] { "device-99" },
            LastKnownLatitude: -26.2041,
            LastKnownLongitude: 28.0473,
            AverageTransactionAmount: 500m);

        _decisionStoreMock.Setup(x => x.GetExistingDecisionAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((FraudDecision?)null);
        _behaviourStoreMock.Setup(x => x.GetBehaviourAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(behaviour);
        _ruleRepositoryMock.Setup(x => x.LoadActiveRulesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<RuleDefinition>
            {
                new(Guid.NewGuid(), "Amount", RuleReasons.HighAmountAnomaly, 30, true),  // 2500/500 = 5x > 3x
                new(Guid.NewGuid(), "Velocity", RuleReasons.HighVelocity, 25, true),      // 7 > 5
                new(Guid.NewGuid(), "Device", RuleReasons.NewDevice, 20, true),           // device-1 not in known
            });

        var sut = CreateSut();
        var result = await sut.ExecuteAsync(request);

        Assert.True(result.IsSuccess);
        // 30 + 25 + 20 = 75 → Reject (≥ 70)
        Assert.Equal(75, result.Value.RiskScore);
        Assert.Equal(DecisionOutcome.Reject, result.Value.Outcome);
        Assert.Equal(3, result.Value.Reasons.Count);
    }

    [Fact]
    public async Task ExecuteAsync_ReturnsFailure_WhenPersistFails_AndFailClosed()
    {
        var request = DefaultRequest();
        _decisionStoreMock.Setup(x => x.GetExistingDecisionAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((FraudDecision?)null);
        _behaviourStoreMock.Setup(x => x.GetBehaviourAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((BehaviouralContext?)null);
        _ruleRepositoryMock.Setup(x => x.LoadActiveRulesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<RuleDefinition>());
        _decisionStoreMock.Setup(x => x.PersistDecisionAsync(It.IsAny<Guid>(), It.IsAny<FraudDecision>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("DB down"));

        var sut = CreateSut(failBehaviour: new FailBehaviourConfig(FailOpen: false));
        var result = await sut.ExecuteAsync(request);

        Assert.True(result.IsFailure);
        Assert.IsType<Error.Infrastructure>(result.Error!);
    }

    [Fact]
    public async Task ExecuteAsync_ReturnsApprove_WhenPersistFails_AndFailOpen()
    {
        var request = DefaultRequest();
        _decisionStoreMock.Setup(x => x.GetExistingDecisionAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((FraudDecision?)null);
        _behaviourStoreMock.Setup(x => x.GetBehaviourAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((BehaviouralContext?)null);
        _ruleRepositoryMock.Setup(x => x.LoadActiveRulesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<RuleDefinition>());
        _decisionStoreMock.Setup(x => x.PersistDecisionAsync(It.IsAny<Guid>(), It.IsAny<FraudDecision>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("DB down"));

        var sut = CreateSut(failBehaviour: new FailBehaviourConfig(FailOpen: true));
        var result = await sut.ExecuteAsync(request);

        Assert.True(result.IsSuccess);
        Assert.Equal(DecisionOutcome.Approve, result.Value.Outcome);
        Assert.Contains("AUDIT_FALLBACK_FAIL_OPEN", result.Value.Reasons);
    }

    [Fact]
    public async Task ExecuteAsync_CatchesBehaviourStoreUnavailable_AndDegradesGracefully()
    {
        var request = DefaultRequest();
        _decisionStoreMock.Setup(x => x.GetExistingDecisionAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((FraudDecision?)null);
        _behaviourStoreMock.Setup(x => x.GetBehaviourAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new BehaviourStoreUnavailableException("Redis down"));
        _ruleRepositoryMock.Setup(x => x.LoadActiveRulesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<RuleDefinition>());

        var sut = CreateSut();
        var result = await sut.ExecuteAsync(request);

        Assert.True(result.IsSuccess);
        Assert.Equal(DecisionOutcome.Approve, result.Value.Outcome);
    }

    [Fact]
    public async Task ExecuteAsync_ProducesApprove_WhenNoRulesMatch()
    {
        var request = DefaultRequest();
        _decisionStoreMock.Setup(x => x.GetExistingDecisionAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((FraudDecision?)null);
        _behaviourStoreMock.Setup(x => x.GetBehaviourAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BehaviouralContext(0, "192.168.1.1", new[] { "device-1" }, null, null, 2500m));
        _ruleRepositoryMock.Setup(x => x.LoadActiveRulesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<RuleDefinition>());

        var sut = CreateSut();
        var result = await sut.ExecuteAsync(request);

        Assert.True(result.IsSuccess);
        Assert.Equal(0, result.Value.RiskScore);
        Assert.Equal(DecisionOutcome.Approve, result.Value.Outcome);
        Assert.Empty(result.Value.Reasons);
    }
}
