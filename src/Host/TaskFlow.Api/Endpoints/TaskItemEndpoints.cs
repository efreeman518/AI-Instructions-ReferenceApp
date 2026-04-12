using Microsoft.AspNetCore.Mvc;
using EF.Common.Contracts;
using TaskFlow.Application.Contracts.Services;
using TaskFlow.Application.Models;

namespace TaskFlow.Api.Endpoints;

public static class TaskItemEndpoints
{
    public static IEndpointRouteBuilder MapTaskItemEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/task-items").WithTags("TaskItems");

        group.MapGet("/{id:guid}", async (Guid id, [FromServices] ITaskItemService service, CancellationToken ct) =>
        {
            var result = await service.GetAsync(id, ct);
            if (result.IsNone) return Results.NotFound();
            if (result.IsFailure) return Results.Problem(result.ErrorMessage);
            return Results.Ok(result.Value!.Item);
        }).WithName("GetTaskItem");

        group.MapPost("/", async ([FromBody] TaskItemDto dto, [FromServices] ITaskItemService service, CancellationToken ct) =>
        {
            var result = await service.CreateAsync(new DefaultRequest<TaskItemDto> { Item = dto }, ct);
            if (result.IsFailure) return Results.BadRequest(result.ErrorMessage);
            return Results.Created($"/api/task-items/{result.Value!.Item!.Id}", result.Value.Item);
        }).WithName("CreateTaskItem");

        group.MapPut("/{id:guid}", async (Guid id, [FromBody] TaskItemDto dto, [FromServices] ITaskItemService service, CancellationToken ct) =>
        {
            dto.Id = id;
            var result = await service.UpdateAsync(new DefaultRequest<TaskItemDto> { Item = dto }, ct);
            if (result.IsFailure) return Results.BadRequest(result.ErrorMessage);
            if (result.Value?.Item == null) return Results.NotFound();
            return Results.Ok(result.Value.Item);
        }).WithName("UpdateTaskItem");

        group.MapDelete("/{id:guid}", async (Guid id, [FromServices] ITaskItemService service, CancellationToken ct) =>
        {
            var result = await service.DeleteAsync(id, ct);
            if (result.IsFailure) return Results.Problem(result.ErrorMessage);
            return Results.NoContent();
        }).WithName("DeleteTaskItem");

        group.MapPost("/search", async ([FromBody] SearchRequest<TaskItemSearchFilter> request, [FromServices] ITaskItemService service, CancellationToken ct) =>
        {
            var response = await service.SearchAsync(request, ct);
            return Results.Ok(response);
        }).WithName("SearchTaskItems");

        return app;
    }
}
