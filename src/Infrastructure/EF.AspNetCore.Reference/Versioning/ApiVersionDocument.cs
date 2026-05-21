using Asp.Versioning;

namespace EF.AspNetCore.Versioning;

public sealed record ApiVersionDocument(ApiVersion Version, string GroupName)
{
    public string DisplayName { get; init; } = GroupName;
}
