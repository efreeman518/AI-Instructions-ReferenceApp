using TaskFlow.Application.Models;
using TaskFlow.Domain.Shared.Enums;

namespace Test.Support.Builders;

/// <summary>Builds task item DTO test data with sensible defaults so tests only override relevant fields.</summary>
public class TaskItemDtoBuilder
{
    private Guid? _id = Guid.NewGuid();
    private string _title = "Test Task";
    private string? _description = "Test task description";
    private Priority _priority = Priority.Medium;
    private TaskItemStatus _status = TaskItemStatus.Open;

    /// <summary>Sets ID on the builder so tests can override only scenario-specific values.</summary>
    public TaskItemDtoBuilder WithId(Guid? id) { _id = id; return this; }
    /// <summary>Sets title on the builder so tests can override only scenario-specific values.</summary>
    public TaskItemDtoBuilder WithTitle(string title) { _title = title; return this; }
    /// <summary>Sets description on the builder so tests can override only scenario-specific values.</summary>
    public TaskItemDtoBuilder WithDescription(string? description) { _description = description; return this; }
    /// <summary>Sets priority on the builder so tests can override only scenario-specific values.</summary>
    public TaskItemDtoBuilder WithPriority(Priority priority) { _priority = priority; return this; }
    /// <summary>Sets status on the builder so tests can override only scenario-specific values.</summary>
    public TaskItemDtoBuilder WithStatus(TaskItemStatus status) { _status = status; return this; }

    /// <summary>Builds test data used by focused test cases.</summary>
    public TaskItemDto Build() => new()
    {
        Id = _id,
        Title = _title,
        Description = _description,
        Priority = _priority,
        Status = _status
    };
}
