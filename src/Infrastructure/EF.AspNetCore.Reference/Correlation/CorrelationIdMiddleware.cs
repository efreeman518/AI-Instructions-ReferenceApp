using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace EF.AspNetCore.Correlation;

public sealed class CorrelationIdMiddleware(
    RequestDelegate next,
    IOptions<CorrelationIdSettings> options)
{
    public const string DefaultHeaderName = "X-Correlation-Id";

    public async Task InvokeAsync(HttpContext context)
    {
        var headerName = string.IsNullOrWhiteSpace(options.Value.HeaderName)
            ? DefaultHeaderName
            : options.Value.HeaderName;
        var correlationId = context.Request.Headers[headerName].FirstOrDefault()
            ?? Activity.Current?.Id
            ?? Guid.NewGuid().ToString();

        context.TraceIdentifier = correlationId;
        context.Request.Headers[headerName] = correlationId;
        context.Response.Headers[headerName] = correlationId;

        await next(context);
    }
}
