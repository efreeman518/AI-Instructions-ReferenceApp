namespace TaskFlow.Application.Models;

/// <summary>Carries default request CQRS data between endpoints and handlers.</summary>
public record DefaultRequest<T>
{
    public required T Item { get; init; }
}
