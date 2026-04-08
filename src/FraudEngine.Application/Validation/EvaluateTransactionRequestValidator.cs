using FraudEngine.Contracts;
using FluentValidation;

namespace FraudEngine.Application.Validation;

/// <summary>
/// Validates the incoming EvaluateTransactionRequest before it reaches the use case.
/// </summary>
public sealed class EvaluateTransactionRequestValidator : AbstractValidator<EvaluateTransactionRequest>
{
    public EvaluateTransactionRequestValidator()
    {
        RuleFor(x => x.TransactionId)
            .NotEmpty().WithMessage("TransactionId is required.");

        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("UserId is required.");

        RuleFor(x => x.Amount)
            .GreaterThan(0).WithMessage("Amount must be greater than zero.");

        RuleFor(x => x.Currency)
            .NotEmpty().WithMessage("Currency is required.")
            .Length(3).WithMessage("Currency must be a 3-character ISO code.");

        RuleFor(x => x.Timestamp)
            .NotEmpty().WithMessage("Timestamp is required.");

        RuleFor(x => x.IpAddress)
            .NotEmpty().WithMessage("IpAddress is required.");

        RuleFor(x => x.DeviceId)
            .NotEmpty().WithMessage("DeviceId is required.");

        RuleFor(x => x.MerchantId)
            .NotEmpty().WithMessage("MerchantId is required.");
    }
}
