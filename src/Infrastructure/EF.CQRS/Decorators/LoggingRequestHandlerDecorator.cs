using EF.CQRS.Abstractions;
using Microsoft.Extensions.Logging;

namespace EF.CQRS.Decorators;

/// <summary>
/// Logs request-handler start, completion, and cancellation.
/// </summary>
public sealed class LoggingRequestHandlerDecorator<TRequest, TResponse>(
    IRequestHandler<TRequest, TResponse> inner,
    ILogger<LoggingRequestHandlerDecorator<TRequest, TResponse>> logger)
    : IRequestHandler<TRequest, TResponse>
{
    public async Task<TResponse> HandleAsync(TRequest request, CancellationToken ct = default)
    {
        logger.LogDebug("Handling CQRS request {RequestType}", typeof(TRequest).Name);

        try
        {
            var response = await inner.HandleAsync(request, ct);
            logger.LogDebug("Handled CQRS request {RequestType}", typeof(TRequest).Name);
            return response;
        }
        catch (OperationCanceledException)
        {
            logger.LogDebug("CQRS request {RequestType} cancelled.", typeof(TRequest).Name);
            throw;
        }
    }
}
