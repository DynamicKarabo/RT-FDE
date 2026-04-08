using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace FraudEngine.Api.Middleware;

/// <summary>
/// Catches all unhandled exceptions and returns RFC 7807 ProblemDetails — never a stack trace.
/// Writes standardized application/problem+json payloads via HttpResponse.WriteAsJsonAsync.
/// </summary>
public sealed class GlobalExceptionFilter : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionFilter> _logger;

    public GlobalExceptionFilter(ILogger<GlobalExceptionFilter> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var correlationId = httpContext.TraceIdentifier;

        _logger.LogError(
            exception,
            "Unhandled exception for request {CorrelationId}. Path: {Path}",
            correlationId, httpContext.Request.Path);

        httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;

        var problemDetails = new ProblemDetails
        {
            Status = StatusCodes.Status500InternalServerError,
            Title = "Internal Server Error",
            Detail = "An internal error occurred. Please contact support with the correlation ID.",
            Instance = httpContext.Request.Path,
            Extensions =
            {
                ["correlationId"] = correlationId,
                ["traceId"] = httpContext.TraceIdentifier
            }
        };

        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

        return true;
    }
}
