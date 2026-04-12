namespace TaskFlow.Uno.Core.Business.Models;

public record TagModel
{
    public Guid? Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Color { get; init; }
}
