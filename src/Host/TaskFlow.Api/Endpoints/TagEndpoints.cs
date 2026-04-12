using Microsoft.AspNetCore.Mvc;
using EF.Common.Contracts;
using TaskFlow.Application.Contracts.Services;
using TaskFlow.Application.Models;

namespace TaskFlow.Api.Endpoints;

public static class TagEndpoints
{
    public static IEndpointRouteBuilder MapTagEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/tags").WithTags("Tags");

        group.MapGet("/{id:guid}", async (Guid id, [FromServices] ITagService service, CancellationToken ct) =>
        {
            var result = await service.GetAsync(id, ct);
            if (result.IsNone) return Results.NotFound();
            if (result.IsFailure) return Results.Problem(result.ErrorMessage);
            return Results.Ok(result.Value!.Item);
        }).WithName("GetTag");

        group.MapPost("/", async ([FromBody] TagDto dto, [FromServices] ITagService service, CancellationToken ct) =>
        {
            var result = await service.CreateAsync(new DefaultRequest<TagDto> { Item = dto }, ct);
            if (result.IsFailure) return Results.BadRequest(result.ErrorMessage);
            return Results.Created($"/api/tags/{result.Value!.Item!.Id}", result.Value.Item);
        }).WithName("CreateTag");

        group.MapPut("/{id:guid}", async (Guid id, [FromBody] TagDto dto, [FromServices] ITagService service, CancellationToken ct) =>
        {
            dto.Id = id;
            var result = await service.UpdateAsync(new DefaultRequest<TagDto> { Item = dto }, ct);
            if (result.IsFailure) return Results.BadRequest(result.ErrorMessage);
            if (result.Value?.Item == null) return Results.NotFound();
            return Results.Ok(result.Value.Item);
        }).WithName("UpdateTag");

        group.MapDelete("/{id:guid}", async (Guid id, [FromServices] ITagService service, CancellationToken ct) =>
        {
            var result = await service.DeleteAsync(id, ct);
            if (result.IsFailure) return Results.Problem(result.ErrorMessage);
            return Results.NoContent();
        }).WithName("DeleteTag");

        group.MapPost("/search", async ([FromBody] SearchRequest<TagSearchFilter> request, [FromServices] ITagService service, CancellationToken ct) =>
        {
            var response = await service.SearchAsync(request, ct);
            return Results.Ok(response);
        }).WithName("SearchTags");

        return app;
    }
}
