using FraudEngine.Application.EvaluateTransaction;
using FraudEngine.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace FraudEngine.Api.Controllers;

[ApiController]
[Route("v1/[controller]")]
public sealed class FraudController : ControllerBase
{
    private readonly IEvaluateTransactionUseCase _useCase;

    public FraudController(IEvaluateTransactionUseCase useCase)
    {
        _useCase = useCase;
    }

    [HttpPost("evaluate")]
    public async Task<ActionResult<FraudDecisionResponse>> EvaluateAsync(
        [FromBody] EvaluateTransactionRequest request,
        CancellationToken ct)
    {
        var result = await _useCase.ExecuteAsync(request, ct);

        if (result.IsFailure)
        {
            return StatusCode(500, new { error = result.Error?.Message });
        }

        return Ok(FraudDecisionResponse.FromDomain(result.Value));
    }
}
