using TaskFlow.Domain.Model;

namespace Test.Support.Builders;

public class TagBuilder
{
    private Guid _tenantId = TestConstants.TenantId;
    private string _name = "Test Tag";
    private string? _color = "#FF0000";

    public TagBuilder WithTenantId(Guid tenantId) { _tenantId = tenantId; return this; }
    public TagBuilder WithName(string name) { _name = name; return this; }
    public TagBuilder WithColor(string? color) { _color = color; return this; }

    public Tag Build()
    {
        var result = Tag.Create(_tenantId, _name, _color);
        return result.Value!;
    }
}
