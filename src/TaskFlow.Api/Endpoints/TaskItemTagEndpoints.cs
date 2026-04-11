using Microsoft.AspNetCore.Mvc;
using EF.Common.Contracts;
using TaskFlow.Application.Contracts.Services;
using TaskFlow.Application.Models;

namespace TaskFlow.Api.Endpoints;

public static class TaskItemTagEndpoints
{
    public static IEndpointRouteBuilder MapTaskItemTagEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/task-item-tags").WithTags("TaskItemTags");

        group.MapGet("/{id:guid}", async (Guid id, [FromServices] ITaskItemTagService service, CancellationToken ct) =>
        {
            var result = await service.GetAsync(id, ct);
            if (result.IsNone) return Results.NotFound();
            if (result.IsFailure) return Results.Problem(result.ErrorMessage);
            return Results.Ok(result.Value!.Item);
        }).WithName("GetTaskItemTag");

        group.MapPost("/", async ([FromBody] TaskItemTagDto dto, [FromServices] ITaskItemTagService service, CancellationToken ct) =>
        {
            var result = await service.CreateAsync(new DefaultRequest<TaskItemTagDto> { Item = dto }, ct);
            if (result.IsFailure) return Results.BadRequest(result.ErrorMessage);
            return Results.Created($"/api/task-item-tags/{result.Value!.Item!.Id}", result.Value.Item);
        }).WithName("CreateTaskItemTag");

        group.MapDelete("/{id:guid}", async (Guid id, [FromServices] ITaskItemTagService service, CancellationToken ct) =>
        {
            var result = await service.DeleteAsync(id, ct);
            if (result.IsFailure) return Results.Problem(result.ErrorMessage);
            return Results.NoContent();
        }).WithName("DeleteTaskItemTag");

        return app;
    }
}
