using FraudEngine.Domain;

namespace FraudEngine.Domain.Interfaces;

/// <summary>
/// Provides ephemeral behavioural state for a user (velocity, devices, last IP).
/// Implementations back this with Redis.
/// </summary>
public interface IBehaviourStore
{
    Task<BehaviouralContext?> GetBehaviourAsync(Guid userId, Guid transactionId, CancellationToken ct = default);
}
