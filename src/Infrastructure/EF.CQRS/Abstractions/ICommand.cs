namespace EF.CQRS.Abstractions;

/// <summary>
/// Marker for a write-side request.
/// </summary>
public interface ICommand<TResponse>
{
}
