namespace TaskFlow.Application.Models;

public class DefaultResponse<T> where T : class
{
    public T? Item { get; set; }
}
