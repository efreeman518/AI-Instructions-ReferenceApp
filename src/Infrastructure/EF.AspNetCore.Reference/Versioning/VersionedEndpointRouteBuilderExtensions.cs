using Asp.Versioning;
using Asp.Versioning.Builder;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace EF.AspNetCore.Versioning;

public static class VersionedEndpointRouteBuilderExtensions
{
    public static ApiVersionSet BuildApiVersionSet(
        this IEndpointRouteBuilder endpoints,
        IEnumerable<ApiVersionDocument> documents)
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        ArgumentNullException.ThrowIfNull(documents);

        var builder = endpoints.NewApiVersionSet()
            .ReportApiVersions();

        foreach (var document in documents)
        {
            builder.HasApiVersion(document.Version);
        }

        return builder.Build();
    }

    public static RouteGroupBuilder MapVersionedApiGroup(
        this IEndpointRouteBuilder endpoints,
        string routePrefix,
        ApiVersionSet versionSet,
        ApiVersion apiVersion)
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        ArgumentException.ThrowIfNullOrWhiteSpace(routePrefix);
        ArgumentNullException.ThrowIfNull(versionSet);
        ArgumentNullException.ThrowIfNull(apiVersion);

        return endpoints.MapGroup(routePrefix)
            .WithApiVersionSet(versionSet)
            .MapToApiVersion(apiVersion);
    }
}
