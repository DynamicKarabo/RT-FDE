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
    [ProducesResponseType(typeof(FraudDecisionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> EvaluateAsync(
        [FromBody] EvaluateTransactionRequest request,
        CancellationToken ct)
    {
        var result = await _useCase.ExecuteAsync(request, ct);

        if (result.IsFailure)
        {
            return Problem(
                detail: result.Error?.Message,
                statusCode: StatusCodes.Status500InternalServerError,
                title: "Fraud evaluation failed");
        }

        return Ok(FraudDecisionResponse.FromDomain(result.Value));
    }
}
