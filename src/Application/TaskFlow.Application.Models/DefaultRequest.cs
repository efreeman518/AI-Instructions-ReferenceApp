namespace TaskFlow.Application.Models;

public record DefaultRequest<T>
{
    public T Item { get; set; } = default!;
}
