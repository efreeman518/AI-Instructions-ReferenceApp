namespace TaskFlow.Application.Models;

public class TagDto
{
    public Guid? Id { get; set; }
    public string Name { get; set; } = null!;
    public string? Color { get; set; }
}
