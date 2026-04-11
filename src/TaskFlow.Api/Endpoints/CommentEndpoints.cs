using Microsoft.AspNetCore.Mvc;
using EF.Common.Contracts;
using TaskFlow.Application.Contracts.Services;
using TaskFlow.Application.Models;

namespace TaskFlow.Api.Endpoints;

public static class CommentEndpoints
{
    public static IEndpointRouteBuilder MapCommentEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/comments").WithTags("Comments");

        group.MapGet("/{id:guid}", async (Guid id, [FromServices] ICommentService service, CancellationToken ct) =>
        {
            var result = await service.GetAsync(id, ct);
            if (result.IsNone) return Results.NotFound();
            if (result.IsFailure) return Results.Problem(result.ErrorMessage);
            return Results.Ok(result.Value!.Item);
        }).WithName("GetComment");

        group.MapPost("/", async ([FromBody] CommentDto dto, [FromServices] ICommentService service, CancellationToken ct) =>
        {
            var result = await service.CreateAsync(new DefaultRequest<CommentDto> { Item = dto }, ct);
            if (result.IsFailure) return Results.BadRequest(result.ErrorMessage);
            return Results.Created($"/api/comments/{result.Value!.Item!.Id}", result.Value.Item);
        }).WithName("CreateComment");

        group.MapPut("/{id:guid}", async (Guid id, [FromBody] CommentDto dto, [FromServices] ICommentService service, CancellationToken ct) =>
        {
            dto.Id = id;
            var result = await service.UpdateAsync(new DefaultRequest<CommentDto> { Item = dto }, ct);
            if (result.IsFailure) return Results.BadRequest(result.ErrorMessage);
            if (result.Value?.Item == null) return Results.NotFound();
            return Results.Ok(result.Value.Item);
        }).WithName("UpdateComment");

        group.MapDelete("/{id:guid}", async (Guid id, [FromServices] ICommentService service, CancellationToken ct) =>
        {
            var result = await service.DeleteAsync(id, ct);
            if (result.IsFailure) return Results.Problem(result.ErrorMessage);
            return Results.NoContent();
        }).WithName("DeleteComment");

        group.MapPost("/search", async ([FromBody] SearchRequest<CommentSearchFilter> request, [FromServices] ICommentService service, CancellationToken ct) =>
        {
            var response = await service.SearchAsync(request, ct);
            return Results.Ok(response);
        }).WithName("SearchComments");

        return app;
    }
}
