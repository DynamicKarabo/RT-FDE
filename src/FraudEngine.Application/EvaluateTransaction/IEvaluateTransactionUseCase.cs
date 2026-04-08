using FraudEngine.Contracts;
using FraudEngine.Domain;
using FraudEngine.Domain.Errors;
using FraudEngine.Domain.Interfaces;
using FraudEngine.Domain.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FraudEngine.Application.EvaluateTransaction;

public interface IEvaluateTransactionUseCase
{
    Task<Result<FraudDecision>> ExecuteAsync(EvaluateTransactionRequest request, CancellationToken ct = default);
}
