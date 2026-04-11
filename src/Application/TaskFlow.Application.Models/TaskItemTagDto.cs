namespace TaskFlow.Application.Models;

public class TaskItemTagDto
{
    public Guid? Id { get; set; }
    public Guid TaskItemId { get; set; }
    public Guid TagId { get; set; }
}
