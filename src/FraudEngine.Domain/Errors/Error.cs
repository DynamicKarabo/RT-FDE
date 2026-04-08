namespace FraudEngine.Domain.Errors;

/// <summary>
/// Discriminated union-style error types for domain operations.
/// </summary>
public abstract record Error(string Message)
{
    public sealed record NotFound(string Detail) : Error(Detail);
    public sealed record Infrastructure(string Detail) : Error(Detail);
    public sealed record Validation(string Detail) : Error(Detail);
}
