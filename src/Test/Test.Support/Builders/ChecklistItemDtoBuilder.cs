using TaskFlow.Application.Models;

namespace Test.Support.Builders;

/// <summary>Builds checklist item DTO test data with sensible defaults so tests only override relevant fields.</summary>
public class ChecklistItemDtoBuilder
{
    private Guid? _id = Guid.NewGuid();
    private string _title = "Test checklist item";
    private bool _isCompleted;
    private int _sortOrder;
    private Guid _taskItemId = Guid.NewGuid();

    /// <summary>Sets ID on the builder so tests can override only scenario-specific values.</summary>
    public ChecklistItemDtoBuilder WithId(Guid? id) { _id = id; return this; }
    /// <summary>Sets title on the builder so tests can override only scenario-specific values.</summary>
    public ChecklistItemDtoBuilder WithTitle(string title) { _title = title; return this; }
    /// <summary>Sets is completed on the builder so tests can override only scenario-specific values.</summary>
    public ChecklistItemDtoBuilder WithIsCompleted(bool isCompleted) { _isCompleted = isCompleted; return this; }
    /// <summary>Sets sort order on the builder so tests can override only scenario-specific values.</summary>
    public ChecklistItemDtoBuilder WithSortOrder(int sortOrder) { _sortOrder = sortOrder; return this; }
    /// <summary>Sets task item ID on the builder so tests can override only scenario-specific values.</summary>
    public ChecklistItemDtoBuilder WithTaskItemId(Guid taskItemId) { _taskItemId = taskItemId; return this; }

    /// <summary>Builds test data used by focused test cases.</summary>
    public ChecklistItemDto Build() => new()
    {
        Id = _id,
        Title = _title,
        IsCompleted = _isCompleted,
        SortOrder = _sortOrder,
        TaskItemId = _taskItemId
    };
}
