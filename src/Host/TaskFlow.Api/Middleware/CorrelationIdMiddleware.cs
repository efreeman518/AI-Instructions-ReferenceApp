using System.Diagnostics;

namespace TaskFlow.Api.Middleware;

public class CorrelationIdMiddleware(RequestDelegate next)
{
    public const string HeaderName = "X-Correlation-Id";

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = context.Request.Headers[HeaderName].FirstOrDefault()
            ?? Activity.Current?.Id
            ?? Guid.NewGuid().ToString();

        context.TraceIdentifier = correlationId;
        context.Request.Headers[HeaderName] = correlationId;
        context.Response.Headers[HeaderName] = correlationId;

        await next(context);
    }
}
