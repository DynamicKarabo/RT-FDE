using FraudEngine.Domain;

namespace FraudEngine.Domain.Interfaces;

/// <summary>
/// Appends an immutable fraud decision record to persistent storage.
/// No updates, no deletes.
/// </summary>
public interface IFraudDecisionStore
{
    Task<FraudDecision?> GetExistingDecisionAsync(Guid transactionId, CancellationToken ct = default);
    Task PersistDecisionAsync(Guid transactionId, FraudDecision decision, CancellationToken ct = default);
}
