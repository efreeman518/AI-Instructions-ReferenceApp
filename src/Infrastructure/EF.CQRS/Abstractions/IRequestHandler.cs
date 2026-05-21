namespace EF.CQRS.Abstractions;

/// <summary>
/// Handles one command or query request type.
/// </summary>
public interface IRequestHandler<in TRequest, TResponse>
{
    Task<TResponse> HandleAsync(TRequest request, CancellationToken ct = default);
}
