using EF.CQRS.Abstractions;
using EF.CQRS.Validation;
using Microsoft.Extensions.Logging;

namespace EF.CQRS.Decorators;

/// <summary>
/// Runs registered request validators before the inner handler.
/// </summary>
public sealed class ValidationRequestHandlerDecorator<TRequest, TResponse>(
    IRequestHandler<TRequest, TResponse> inner,
    IEnumerable<IRequestValidator<TRequest>> validators,
    IValidationFailureResponseFactory<TResponse> failureResponseFactory,
    ILogger<ValidationRequestHandlerDecorator<TRequest, TResponse>> logger)
    : IRequestHandler<TRequest, TResponse>
{
    public async Task<TResponse> HandleAsync(TRequest request, CancellationToken ct = default)
    {
        var errors = new List<string>();

        foreach (var validator in validators)
        {
            var result = await validator.ValidateAsync(request, ct);
            if (!result.IsValid)
            {
                errors.AddRange(result.Errors);
            }
        }

        if (errors.Count == 0)
        {
            return await inner.HandleAsync(request, ct);
        }

        logger.LogDebug(
            "CQRS request {RequestType} failed validation with {ErrorCount} error(s).",
            typeof(TRequest).Name,
            errors.Count);

        return failureResponseFactory.CreateFailure(errors);
    }
}
