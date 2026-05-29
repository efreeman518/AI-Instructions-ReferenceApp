using Asp.Versioning;

namespace TaskFlow.Api;

/// <summary>Configures API contract host behavior for TaskFlow runtime services.</summary>
internal static class ApiContract
{
    public const string Title = "TaskFlow API";
    public const string Description = "Multi-tenant TaskFlow API";
    public const string ApiExplorerGroupNameFormat = "'v'VVV";
    public const string VersionedRoutePrefix = "/api/v{apiVersion:apiVersion}";

    public static readonly ApiDocument V1 = new(new ApiVersion(1, 0), "v1");
    public static readonly IReadOnlyList<ApiDocument> SupportedDocuments = [V1];

    public static ApiVersion DefaultVersion => V1.Version;
    public static string DefaultGroupName => V1.GroupName;
}

/// <summary>Configures API document host behavior for TaskFlow runtime services.</summary>
internal sealed record ApiDocument(ApiVersion Version, string GroupName)
{
    public string DisplayName => GroupName;
}
