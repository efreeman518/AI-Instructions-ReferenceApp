using TaskFlow.Application.Models;

namespace Test.Support.Builders;

public class ChecklistItemDtoBuilder
{
    private Guid? _id = Guid.NewGuid();
    private string _title = "Test checklist item";
    private bool _isCompleted;
    private int _sortOrder;
    private Guid _taskItemId = Guid.NewGuid();

    public ChecklistItemDtoBuilder WithId(Guid? id) { _id = id; return this; }
    public ChecklistItemDtoBuilder WithTitle(string title) { _title = title; return this; }
    public ChecklistItemDtoBuilder WithIsCompleted(bool isCompleted) { _isCompleted = isCompleted; return this; }
    public ChecklistItemDtoBuilder WithSortOrder(int sortOrder) { _sortOrder = sortOrder; return this; }
    public ChecklistItemDtoBuilder WithTaskItemId(Guid taskItemId) { _taskItemId = taskItemId; return this; }

    public ChecklistItemDto Build() => new()
    {
        Id = _id,
        Title = _title,
        IsCompleted = _isCompleted,
        SortOrder = _sortOrder,
        TaskItemId = _taskItemId
    };
}
