using EF.Common.Contracts;
using EF.CQRS.Validation;
using EF.Data.Contracts;
using Microsoft.Extensions.Logging;
using TaskFlow.Application.Contracts.Messaging;

namespace TaskFlow.Application.Cqrs.Shared;

internal static class CqrsHandlerSupport
{
    public static async Task<PagedResponse<TDto>> SearchAsync<TDto>(
        Func<CancellationToken, Task<PagedResponse<TDto>>> search,
        ILogger logger,
        string operation,
        CancellationToken ct)
    {
        try
        {
            return await search(ct);
        }
        catch (OperationCanceledException)
        {
            logger.LogDebug("{Operation} search cancelled by client.", operation);
            return new PagedResponse<TDto>();
        }
    }

    public static async Task<Result> TrySaveAsync(
        IRepositoryBase repository,
        ILogger logger,
        string errorMessage,
        CancellationToken ct,
        params object?[] args)
    {
        try
        {
            await repository.SaveChangesAsync(OptimisticConcurrencyWinner.ClientWins, ct);
            return Result.Success();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, errorMessage, args);
            return Result.Failure(ex.GetBaseException().Message);
        }
    }

    public static async Task TryPublishAsync<TEvent>(
        IIntegrationEventPublisher eventPublisher,
        TEvent integrationEvent,
        string? correlationId,
        ILogger logger,
        string path,
        CancellationToken ct)
        where TEvent : class
    {
        try
        {
            await eventPublisher.PublishAsync(integrationEvent, correlationId, ct);
        }
        catch (Exception ex)
        {
            logger.LogWarning(
                ex,
                "Failed to publish {Event} for {Path} CQRS path; persistence succeeded.",
                typeof(TEvent).Name,
                path);
        }
    }

    public static RequestValidationResult ToValidationResult<T>(Result<T> result)
    {
        if (result.IsSuccess) return RequestValidationResult.Valid();
        if (result.Errors.Count > 0) return RequestValidationResult.Failure(result.Errors);
        return RequestValidationResult.Failure([result.ErrorMessage ?? "Validation failed."]);
    }
}
