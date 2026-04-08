using FraudEngine.Domain;

namespace FraudEngine.Domain.Services;

/// <summary>
/// Evaluates a set of fraud rules against a transaction context and its behavioural state.
/// Pure in-memory computation — zero infrastructure dependencies.
/// </summary>
public sealed class RuleEngine
{
    /// <summary>
    /// Evaluates all active rules against the transaction and returns the matches.
    /// </summary>
    /// <param name="rules">Active rule definitions loaded from storage.</param>
    /// <param name="transaction">The transaction to evaluate.</param>
    /// <param name="behaviour">Behavioural context (may represent degraded mode with reduced data).</param>
    /// <param name="thresholds">Configuration-driven rule evaluation thresholds.</param>
    public IReadOnlyList<RuleMatch> Evaluate(
        IReadOnlyList<RuleDefinition> rules,
        TransactionContext transaction,
        BehaviouralContext? behaviour,
        RuleEvaluationThresholds thresholds)
    {
        var matches = new List<RuleMatch>(rules.Count);

        foreach (var rule in rules)
        {
            if (!rule.IsActive)
                continue;

            if (TryMatch(rule, transaction, behaviour, thresholds, out var match))
            {
                matches.Add(match);
            }
        }

        return matches;
    }

    private static bool TryMatch(
        RuleDefinition rule,
        TransactionContext transaction,
        BehaviouralContext? behaviour,
        RuleEvaluationThresholds thresholds,
        out RuleMatch match)
    {
        switch (rule.RuleReason)
        {
            case RuleReasons.HighAmountAnomaly:
                if (behaviour is not null && behaviour.AverageTransactionAmount > 0)
                {
                    var multiplier = (double)(transaction.Amount / behaviour.AverageTransactionAmount);
                    if (multiplier > thresholds.AmountAnomalyMultiplier)
                    {
                        match = new RuleMatch(rule.RuleReason, rule.ScoreDelta);
                        return true;
                    }
                }
                // If no behavioural data, still flag if amount exceeds absolute threshold
                if (behaviour is null && transaction.Amount > thresholds.HighAmountAbsoluteThreshold)
                {
                    match = new RuleMatch(rule.RuleReason, rule.ScoreDelta);
                    return true;
                }
                break;

            case RuleReasons.HighVelocity:
                if (behaviour is not null && behaviour.TransactionCountLast60s > thresholds.MaxTransactionsPer60s)
                {
                    match = new RuleMatch(rule.RuleReason, rule.ScoreDelta);
                    return true;
                }
                break;

            case RuleReasons.GeoAnomaly:
                if (behaviour is not null &&
                    behaviour.LastKnownLatitude.HasValue &&
                    behaviour.LastKnownLongitude.HasValue &&
                    transaction.LastKnownLatitude.HasValue &&
                    transaction.LastKnownLongitude.HasValue)
                {
                    var distanceKm = HaversineDistance(
                        behaviour.LastKnownLatitude.Value,
                        behaviour.LastKnownLongitude.Value,
                        transaction.LastKnownLatitude.Value,
                        transaction.LastKnownLongitude.Value);

                    if (distanceKm > thresholds.MaxGeoDistanceKm)
                    {
                        match = new RuleMatch(rule.RuleReason, rule.ScoreDelta);
                        return true;
                    }
                }
                break;

            case RuleReasons.NewDevice:
                if (behaviour is not null &&
                    behaviour.KnownDeviceIds.Count > 0 &&
                    !behaviour.KnownDeviceIds.Contains(transaction.DeviceId))
                {
                    match = new RuleMatch(rule.RuleReason, rule.ScoreDelta);
                    return true;
                }
                // No behavioural data = treat as unknown/new device
                if (behaviour is null)
                {
                    match = new RuleMatch(rule.RuleReason, rule.ScoreDelta);
                    return true;
                }
                break;

            default:
                // Unknown rule type — skip to avoid breaking on new rule additions
                break;
        }

        match = default!;
        return false;
    }

    /// <summary>
    /// Haversine formula — returns distance in km between two lat/lon points.
    /// </summary>
    private static double HaversineDistance(double lat1, double lon1, double lat2, double lon2)
    {
        const double earthRadiusKm = 6371.0;
        var dLat = ToRadians(lat2 - lat1);
        var dLon = ToRadians(lon2 - lon1);
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return earthRadiusKm * c;
    }

    private static double ToRadians(double degrees) => degrees * Math.PI / 180.0;
}
