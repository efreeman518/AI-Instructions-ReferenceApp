using Microsoft.AspNetCore.Mvc;
using EF.Common.Contracts;
using TaskFlow.Application.Contracts.Services;
using TaskFlow.Application.Models;

namespace TaskFlow.Api.Endpoints;

public static class ChecklistItemEndpoints
{
    public static IEndpointRouteBuilder MapChecklistItemEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/checklist-items").WithTags("ChecklistItems");

        group.MapGet("/{id:guid}", async (Guid id, [FromServices] IChecklistItemService service, CancellationToken ct) =>
        {
            var result = await service.GetAsync(id, ct);
            if (result.IsNone) return Results.NotFound();
            if (result.IsFailure) return Results.Problem(result.ErrorMessage);
            return Results.Ok(result.Value!.Item);
        }).WithName("GetChecklistItem");

        group.MapPost("/", async ([FromBody] ChecklistItemDto dto, [FromServices] IChecklistItemService service, CancellationToken ct) =>
        {
            var result = await service.CreateAsync(new DefaultRequest<ChecklistItemDto> { Item = dto }, ct);
            if (result.IsFailure) return Results.BadRequest(result.ErrorMessage);
            return Results.Created($"/api/checklist-items/{result.Value!.Item!.Id}", result.Value.Item);
        }).WithName("CreateChecklistItem");

        group.MapPut("/{id:guid}", async (Guid id, [FromBody] ChecklistItemDto dto, [FromServices] IChecklistItemService service, CancellationToken ct) =>
        {
            dto.Id = id;
            var result = await service.UpdateAsync(new DefaultRequest<ChecklistItemDto> { Item = dto }, ct);
            if (result.IsFailure) return Results.BadRequest(result.ErrorMessage);
            if (result.Value?.Item == null) return Results.NotFound();
            return Results.Ok(result.Value.Item);
        }).WithName("UpdateChecklistItem");

        group.MapDelete("/{id:guid}", async (Guid id, [FromServices] IChecklistItemService service, CancellationToken ct) =>
        {
            var result = await service.DeleteAsync(id, ct);
            if (result.IsFailure) return Results.Problem(result.ErrorMessage);
            return Results.NoContent();
        }).WithName("DeleteChecklistItem");

        group.MapPost("/search", async ([FromBody] SearchRequest<ChecklistItemSearchFilter> request, [FromServices] IChecklistItemService service, CancellationToken ct) =>
        {
            var response = await service.SearchAsync(request, ct);
            return Results.Ok(response);
        }).WithName("SearchChecklistItems");

        return app;
    }
}
