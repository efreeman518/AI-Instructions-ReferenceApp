using TaskFlow.Application.Models;

namespace Test.Support.Builders;

public class TaskItemTagDtoBuilder
{
    private Guid? _id = Guid.NewGuid();
    private Guid _taskItemId = Guid.NewGuid();
    private Guid _tagId = Guid.NewGuid();

    public TaskItemTagDtoBuilder WithId(Guid? id) { _id = id; return this; }
    public TaskItemTagDtoBuilder WithTaskItemId(Guid taskItemId) { _taskItemId = taskItemId; return this; }
    public TaskItemTagDtoBuilder WithTagId(Guid tagId) { _tagId = tagId; return this; }

    public TaskItemTagDto Build() => new()
    {
        Id = _id,
        TaskItemId = _taskItemId,
        TagId = _tagId
    };
}
