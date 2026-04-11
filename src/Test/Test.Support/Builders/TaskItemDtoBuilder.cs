using TaskFlow.Application.Models;
using TaskFlow.Domain.Shared.Enums;

namespace Test.Support.Builders;

public class TaskItemDtoBuilder
{
    private Guid? _id = Guid.NewGuid();
    private string _title = "Test Task";
    private string? _description = "Test task description";
    private Priority _priority = Priority.Medium;
    private TaskItemStatus _status = TaskItemStatus.Open;

    public TaskItemDtoBuilder WithId(Guid? id) { _id = id; return this; }
    public TaskItemDtoBuilder WithTitle(string title) { _title = title; return this; }
    public TaskItemDtoBuilder WithDescription(string? description) { _description = description; return this; }
    public TaskItemDtoBuilder WithPriority(Priority priority) { _priority = priority; return this; }
    public TaskItemDtoBuilder WithStatus(TaskItemStatus status) { _status = status; return this; }

    public TaskItemDto Build() => new()
    {
        Id = _id,
        Title = _title,
        Description = _description,
        Priority = _priority,
        Status = _status
    };
}
