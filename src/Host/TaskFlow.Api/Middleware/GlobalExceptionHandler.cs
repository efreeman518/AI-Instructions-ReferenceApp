using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace TaskFlow.Api.Middleware;

internal sealed class DefaultExceptionHandler(
    ILogger<DefaultExceptionHandler> logger,
    IHostEnvironment environment) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var (statusCode, title) = exception switch
        {
            DbUpdateConcurrencyException
                => (StatusCodes.Status409Conflict, "Concurrency conflict"),
            UnauthorizedAccessException
                => (StatusCodes.Status403Forbidden, "Forbidden"),
            OperationCanceledException
                => (499, "Client closed request"),
            BadHttpRequestException
                => (StatusCodes.Status400BadRequest, "Bad request"),
            ArgumentException or FormatException
                => (StatusCodes.Status400BadRequest, "Bad request"),
            _
                => (StatusCodes.Status500InternalServerError, "Internal server error")
        };

        // Client disconnections and bad-request exceptions are not server errors;
        // log them at lower severity to avoid flooding error dashboards.
        if (exception is OperationCanceledException)
            logger.LogInformation("Request cancelled by client: {Path}", httpContext.Request.Path);
        else if (statusCode < 500)
            logger.LogWarning(exception, "Client error {StatusCode}: {ExceptionType} — {Message}",
                statusCode, exception.GetType().Name, exception.Message);
        else
            logger.LogError(exception, "Unhandled exception: {ExceptionType} — {Message}",
                exception.GetType().Name, exception.Message);

        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = environment.IsDevelopment() || environment.IsStaging()
                ? exception.ToString()
                : exception.Message,
            Instance = httpContext.Request.Path
        };

        if (httpContext.Response.HasStarted)
            return true;

        httpContext.Response.StatusCode = statusCode;

        try
        {
            await httpContext.Response.WriteAsJsonAsync(problemDetails, CancellationToken.None);
        }
        catch (OperationCanceledException)
        {
            // Client disconnected while writing the error response.
        }

        return true;
    }
}
