using TaskFlow.Domain.Model;

namespace Test.Support.Builders;

public class TaskItemTagBuilder
{
    private Guid _tenantId = TestConstants.TenantId;
    private Guid _taskItemId = Guid.NewGuid();
    private Guid _tagId = Guid.NewGuid();

    public TaskItemTagBuilder WithTenantId(Guid tenantId) { _tenantId = tenantId; return this; }
    public TaskItemTagBuilder WithTaskItemId(Guid taskItemId) { _taskItemId = taskItemId; return this; }
    public TaskItemTagBuilder WithTagId(Guid tagId) { _tagId = tagId; return this; }

    public TaskItemTag Build()
    {
        var result = TaskItemTag.Create(_tenantId, _taskItemId, _tagId);
        return result.Value!;
    }
}
