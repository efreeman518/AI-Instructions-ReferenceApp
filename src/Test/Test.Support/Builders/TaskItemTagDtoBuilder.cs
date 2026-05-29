using TaskFlow.Application.Models;

namespace Test.Support.Builders;

/// <summary>Builds task item tag DTO test data with sensible defaults so tests only override relevant fields.</summary>
public class TaskItemTagDtoBuilder
{
    private Guid? _id = Guid.NewGuid();
    private Guid _taskItemId = Guid.NewGuid();
    private Guid _tagId = Guid.NewGuid();

    /// <summary>Sets ID on the builder so tests can override only scenario-specific values.</summary>
    public TaskItemTagDtoBuilder WithId(Guid? id) { _id = id; return this; }
    /// <summary>Sets task item ID on the builder so tests can override only scenario-specific values.</summary>
    public TaskItemTagDtoBuilder WithTaskItemId(Guid taskItemId) { _taskItemId = taskItemId; return this; }
    /// <summary>Sets tag ID on the builder so tests can override only scenario-specific values.</summary>
    public TaskItemTagDtoBuilder WithTagId(Guid tagId) { _tagId = tagId; return this; }

    /// <summary>Builds test data used by focused test cases.</summary>
    public TaskItemTagDto Build() => new()
    {
        Id = _id,
        TaskItemId = _taskItemId,
        TagId = _tagId
    };
}
