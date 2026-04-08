using Microsoft.AspNetCore.Diagnostics;
using System.Net.Mime;
using System.Text.Json;

namespace FraudEngine.Api.Middleware;

/// <summary>
/// Catches all unhandled exceptions and returns a 500 with a correlation ID — never a stack trace.
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

        httpContext.Response.StatusCode = 500;
        httpContext.Response.ContentType = MediaTypeNames.Application.Json;

        var body = new { error = "An internal error occurred.", correlationId };
        var json = JsonSerializer.Serialize(body);

        await httpContext.Response.WriteAsync(json, cancellationToken);

        return true;
    }
}
