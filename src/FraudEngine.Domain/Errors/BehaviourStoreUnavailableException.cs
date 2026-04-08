namespace FraudEngine.Domain.Errors;

/// <summary>
/// Thrown when the behavioural store (Redis) is unavailable.
/// Infrastructure implementations should catch Redis-specific exceptions and rethrow as this.
/// Allows the Application layer to catch precisely this without depending on StackExchange.Redis.
/// </summary>
public sealed class BehaviourStoreUnavailableException : Exception
{
    public BehaviourStoreUnavailableException(string message, Exception? inner = null) : base(message, inner) { }
    public BehaviourStoreUnavailableException() { }
}
