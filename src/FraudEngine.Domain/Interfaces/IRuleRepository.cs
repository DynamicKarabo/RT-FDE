using FraudEngine.Domain;

namespace FraudEngine.Domain.Interfaces;

/// <summary>
/// Loads fraud rule definitions from persistent storage.
/// </summary>
public interface IRuleRepository
{
    Task<IReadOnlyList<RuleDefinition>> LoadActiveRulesAsync(CancellationToken ct = default);
}
