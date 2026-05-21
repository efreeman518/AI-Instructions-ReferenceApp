namespace EF.CQRS.Abstractions;

/// <summary>
/// Marker for a read-side request.
/// </summary>
public interface IQuery<TResponse>
{
}
