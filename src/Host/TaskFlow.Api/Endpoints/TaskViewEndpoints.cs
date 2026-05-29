using Microsoft.AspNetCore.Mvc;
using TaskFlow.Application.Contracts.Storage;

namespace TaskFlow.Api.Endpoints;

/// <summary>Maps task view HTTP routes to the selected application implementation and API contract metadata.</summary>
public static class TaskViewEndpoints
{
    /// <summary>Registers task view routes, handlers, and response metadata.</summary>
    public static IEndpointRouteBuilder MapTaskViewEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/task-views").WithTags("TaskViews");

        group.MapGet("/{id}", async (string id,
            [FromQuery] string tenantId,
            [FromServices] ITaskViewRepository repo,
            CancellationToken ct) =>
        {
            var result = await repo.GetAsync(id, tenantId, ct);
            return result is not null ? Results.Ok(result) : Results.NotFound();
        }).WithName("GetTaskView");

        group.MapGet("/", async (
            [FromQuery] string tenantId,
            [FromQuery] int? pageSize,
            [FromServices] ITaskViewRepository repo,
            CancellationToken ct) =>
        {
            var results = await repo.QueryByTenantAsync(tenantId, pageSize ?? 20, ct: ct);
            return Results.Ok(results);
        }).WithName("GetTaskViews");

        return app;
    }
}
