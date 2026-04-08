namespace FraudEngine.Domain;

/// <summary>
/// The final action to take on a transaction.
/// </summary>
public enum DecisionOutcome
{
    Approve = 0,
    Review = 1,
    Reject = 2,
}
