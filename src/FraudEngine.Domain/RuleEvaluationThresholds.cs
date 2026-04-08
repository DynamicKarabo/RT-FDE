namespace FraudEngine.Domain;

/// <summary>
/// Rule evaluation thresholds — configuration-driven tuning knobs.
/// </summary>
public sealed record RuleEvaluationThresholds(
    /// <summary>Amount multiplier above average that triggers HIGH_AMOUNT_ANOMALY (e.g. 3.0 = 3x avg).</summary>
    double AmountAnomalyMultiplier,
    /// <summary>Absolute amount threshold used when behavioural avg is unavailable.</summary>
    decimal HighAmountAbsoluteThreshold,
    /// <summary>Max transactions per 60s before HIGH_VELOCITY fires.</summary>
    int MaxTransactionsPer60s,
    /// <summary>Max geo distance in km within a short window before GEO_ANOMALY fires.</summary>
    double MaxGeoDistanceKm);
