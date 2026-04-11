namespace TaskFlow.Application.Models;

public class DefaultRequest<T> where T : class
{
    public T Item { get; set; } = default!;
}
