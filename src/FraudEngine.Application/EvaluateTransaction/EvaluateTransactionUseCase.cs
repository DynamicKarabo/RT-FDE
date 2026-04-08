using FraudEngine.Contracts;
using FraudEngine.Domain;
using FraudEngine.Domain.Errors;
using FraudEngine.Domain.Interfaces;
using FraudEngine.Domain.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FraudEngine.Application.EvaluateTransaction;

/// <summary>
/// Orchestrates the full fraud evaluation pipeline:
/// 1. Idempotency check (fast path — return cached decision if exists)
/// 2. Fetch behavioural context from Redis (graceful degradation on RedisConnectionException)
/// 3. Load active rules from DB
/// 4. Evaluate rules in memory
/// 5. Compute risk score
/// 6. Map score → decision
/// 7. Persist decision (awaited — audit trail must succeed)
/// </summary>
public sealed class EvaluateTransactionUseCase : IEvaluateTransactionUseCase
{
    private readonly IFraudDecisionStore _decisionStore;
    private readonly IBehaviourStore _behaviourStore;
    private readonly IRuleRepository _ruleRepository;
    private readonly RuleEngine _ruleEngine;
    private readonly RiskScorer _riskScorer;
    private readonly DecisionEngine _decisionEngine;
    private readonly FraudThresholds _thresholds;
    private readonly RuleEvaluationThresholds _ruleThresholds;
    private readonly FailBehaviourConfig _failBehaviour;
    private readonly ILogger<EvaluateTransactionUseCase> _logger;

    public EvaluateTransactionUseCase(
        IFraudDecisionStore decisionStore,
        IBehaviourStore behaviourStore,
        IRuleRepository ruleRepository,
        RuleEngine ruleEngine,
        RiskScorer riskScorer,
        DecisionEngine decisionEngine,
        IOptions<FraudThresholds> thresholds,
        IOptions<RuleEvaluationThresholds> ruleThresholds,
        IOptions<FailBehaviourConfig> failBehaviour,
        ILogger<EvaluateTransactionUseCase> logger)
    {
        _decisionStore = decisionStore;
        _behaviourStore = behaviourStore;
        _ruleRepository = ruleRepository;
        _ruleEngine = ruleEngine;
        _riskScorer = riskScorer;
        _decisionEngine = decisionEngine;
        _thresholds = thresholds.Value;
        _ruleThresholds = ruleThresholds.Value;
        _failBehaviour = failBehaviour.Value;
        _logger = logger;
    }

    public async Task<Result<FraudDecision>> ExecuteAsync(
        EvaluateTransactionRequest request,
        CancellationToken ct = default)
    {
        var correlationId = request.TransactionId.ToString();

        // Step 1: Idempotency check — return cached decision if it exists
        var existing = await _decisionStore.GetExistingDecisionAsync(request.TransactionId, ct);
        if (existing is not null)
        {
            _logger.LogInformation(
                "Idempotent hit for {TransactionId}. CorrelationId: {CorrelationId}. Returning cached decision: {Decision}.",
                request.TransactionId, correlationId, existing.Outcome);
            return Result<FraudDecision>.Success(existing);
        }

        // Step 2: Fetch behavioural context — catch BehaviourStoreUnavailableException specifically.
        // All other exceptions bubble up to the global exception handler middleware.
        BehaviouralContext? behaviour = null;
        try
        {
            behaviour = await _behaviourStore.GetBehaviourAsync(request.UserId, request.TransactionId, ct);
        }
        catch (BehaviourStoreUnavailableException ex)
        {
            _logger.LogWarning(
                ex,
                "Behaviour store unavailable for {TransactionId}. CorrelationId: {CorrelationId}. Proceeding with degraded scoring.",
                request.TransactionId, correlationId);
        }

        // Step 3: Map request → domain TransactionContext
        var transaction = new TransactionContext(
            request.TransactionId,
            request.UserId,
            request.Amount,
            request.Currency,
            request.Timestamp,
            request.IpAddress,
            request.DeviceId,
            request.MerchantId,
            request.Latitude,
            request.Longitude);

        // Step 4: Load active rules
        var rules = await _ruleRepository.LoadActiveRulesAsync(ct);

        // Step 5: Evaluate rules in memory
        var matches = _ruleEngine.Evaluate(rules, transaction, behaviour, _ruleThresholds);

        // Step 6: Compute risk score
        var riskScore = _riskScorer.Compute(matches);

        // Step 7: Map score → decision
        var reasons = matches.Select(m => m.RuleReason).ToList();
        var decision = _decisionEngine.MapScore(riskScore, reasons, _thresholds);

        // Step 8: Persist decision (awaited — must succeed before returning)
        try
        {
            await _decisionStore.PersistDecisionAsync(request.TransactionId, decision, ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(
                ex,
                "Failed to persist decision for {TransactionId}. CorrelationId: {CorrelationId}. Decision: {Decision}. FailBehaviour: {FailBehaviour}.",
                request.TransactionId, correlationId, decision.Outcome, _failBehaviour.FailOpen ? "OPEN" : "CLOSED");

            // Configuration-driven fail behaviour: default CLOSED (block).
            // Operators explicitly opt into OPEN (allow) where justified.
            if (_failBehaviour.FailOpen)
            {
                _logger.LogWarning(
                    "Fail OPEN active. Allowing transaction {TransactionId} despite audit failure. CorrelationId: {CorrelationId}.",
                    request.TransactionId, correlationId);

                var allowDecision = new FraudDecision(
                    DecisionOutcome.Approve,
                    decision.RiskScore,
                    decision.Reasons.Append("AUDIT_FALLBACK_FAIL_OPEN").ToList(),
                    decision.Timestamp);

                return Result<FraudDecision>.Success(allowDecision);
            }

            return Result<FraudDecision>.Failure(
                new Error.Infrastructure("Failed to persist fraud decision — audit trail incomplete. Transaction blocked (fail CLOSED)."));
        }

        _logger.LogInformation(
            "Fraud evaluation complete for {TransactionId}. CorrelationId: {CorrelationId}. Decision: {Decision}, Score: {Score}, Reasons: {Reasons}.",
            request.TransactionId, correlationId, decision.Outcome, decision.RiskScore, string.Join(", ", decision.Reasons));

        return Result<FraudDecision>.Success(decision);
    }
}
