using TaskFlow.Domain.Model;

namespace Test.Support.Builders;

public class ChecklistItemBuilder
{
    private Guid _tenantId = TestConstants.TenantId;
    private Guid _taskItemId = Guid.NewGuid();
    private string _title = "Test Checklist Item";
    private int _sortOrder;

    public ChecklistItemBuilder WithTenantId(Guid tenantId) { _tenantId = tenantId; return this; }
    public ChecklistItemBuilder WithTaskItemId(Guid taskItemId) { _taskItemId = taskItemId; return this; }
    public ChecklistItemBuilder WithTitle(string title) { _title = title; return this; }
    public ChecklistItemBuilder WithSortOrder(int sortOrder) { _sortOrder = sortOrder; return this; }

    public ChecklistItem Build()
    {
        var result = ChecklistItem.Create(_tenantId, _taskItemId, _title, _sortOrder);
        return result.Value!;
    }
}
