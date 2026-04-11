using TaskFlow.Domain.Model;
using TaskFlow.Domain.Shared.Enums;

namespace Test.Support.Builders;

public class TaskItemBuilder
{
    private Guid _tenantId = TestConstants.TenantId;
    private string _title = "Test Task";
    private string? _description = "Test description";
    private Priority _priority = Priority.Medium;
    private Guid? _categoryId;
    private Guid? _parentTaskItemId;

    public TaskItemBuilder WithTenantId(Guid tenantId) { _tenantId = tenantId; return this; }
    public TaskItemBuilder WithTitle(string title) { _title = title; return this; }
    public TaskItemBuilder WithDescription(string? description) { _description = description; return this; }
    public TaskItemBuilder WithPriority(Priority priority) { _priority = priority; return this; }
    public TaskItemBuilder WithCategoryId(Guid? categoryId) { _categoryId = categoryId; return this; }
    public TaskItemBuilder WithParentTaskItemId(Guid? parentTaskItemId) { _parentTaskItemId = parentTaskItemId; return this; }

    public TaskItem Build()
    {
        var result = TaskItem.Create(_tenantId, _title, _description, _priority, _categoryId, _parentTaskItemId);
        return result.Value!;
    }
}
