namespace FraudEngine.Domain;

/// <summary>
/// Configuration-driven fail behaviour.
/// Default is CLOSED (block payments on timeout) — operators explicitly opt into OPEN.
/// </summary>
public sealed record FailBehaviourConfig(bool FailOpen);
