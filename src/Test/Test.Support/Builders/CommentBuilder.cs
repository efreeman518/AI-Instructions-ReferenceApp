using TaskFlow.Domain.Model;

namespace Test.Support.Builders;

public class CommentBuilder
{
    private Guid _tenantId = TestConstants.TenantId;
    private Guid _taskItemId = Guid.NewGuid();
    private string _body = "Test comment body";

    public CommentBuilder WithTenantId(Guid tenantId) { _tenantId = tenantId; return this; }
    public CommentBuilder WithTaskItemId(Guid taskItemId) { _taskItemId = taskItemId; return this; }
    public CommentBuilder WithBody(string body) { _body = body; return this; }

    public Comment Build()
    {
        var result = Comment.Create(_tenantId, _taskItemId, _body);
        return result.Value!;
    }
}
