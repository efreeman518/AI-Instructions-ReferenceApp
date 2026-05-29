using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using TaskFlow.Infrastructure.AI.Search;

namespace TaskFlow.Api.Endpoints;

/// <summary>Maps search HTTP routes to the selected application implementation and API contract metadata.</summary>
public static class SearchEndpoints
{
    /// <summary>Registers search routes, handlers, and response metadata.</summary>
    public static IEndpointRouteBuilder MapSearchEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/search").WithTags("Search");

        group.MapGet("/tasks", async (
            [FromQuery] string query,
            [FromQuery] SearchMode mode,
            [FromQuery] int maxResults,
            [FromServices] ITaskFlowSearchService searchService,
            HttpContext httpContext,
            CancellationToken ct) =>
        {
            if (maxResults <= 0 || maxResults > 50) maxResults = 10;

            var tenantClaim = httpContext.User.FindFirst("tenant_id")?.Value;
            Guid? tenantId = Guid.TryParse(tenantClaim, out var tid) ? tid : null;

            var results = await searchService.SearchTaskItemsAsync(query, mode, tenantId, maxResults, ct);
            return Results.Ok(results);
        }).WithName("SearchTasks");

        return app;
    }
}
