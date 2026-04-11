using Microsoft.AspNetCore.Mvc;
using EF.Common.Contracts;
using TaskFlow.Application.Contracts.Services;
using TaskFlow.Application.Models;

namespace TaskFlow.Api.Endpoints;

public static class AttachmentEndpoints
{
    public static IEndpointRouteBuilder MapAttachmentEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/attachments").WithTags("Attachments");

        group.MapGet("/{id:guid}", async (Guid id, [FromServices] IAttachmentService service, CancellationToken ct) =>
        {
            var result = await service.GetAsync(id, ct);
            if (result.IsNone) return Results.NotFound();
            if (result.IsFailure) return Results.Problem(result.ErrorMessage);
            return Results.Ok(result.Value!.Item);
        }).WithName("GetAttachment");

        group.MapPost("/", async ([FromBody] AttachmentDto dto, [FromServices] IAttachmentService service, CancellationToken ct) =>
        {
            var result = await service.CreateAsync(new DefaultRequest<AttachmentDto> { Item = dto }, ct);
            if (result.IsFailure) return Results.BadRequest(result.ErrorMessage);
            return Results.Created($"/api/attachments/{result.Value!.Item!.Id}", result.Value.Item);
        }).WithName("CreateAttachment");

        group.MapPut("/{id:guid}", async (Guid id, [FromBody] AttachmentDto dto, [FromServices] IAttachmentService service, CancellationToken ct) =>
        {
            dto.Id = id;
            var result = await service.UpdateAsync(new DefaultRequest<AttachmentDto> { Item = dto }, ct);
            if (result.IsFailure) return Results.BadRequest(result.ErrorMessage);
            if (result.Value?.Item == null) return Results.NotFound();
            return Results.Ok(result.Value.Item);
        }).WithName("UpdateAttachment");

        group.MapDelete("/{id:guid}", async (Guid id, [FromServices] IAttachmentService service, CancellationToken ct) =>
        {
            var result = await service.DeleteAsync(id, ct);
            if (result.IsFailure) return Results.Problem(result.ErrorMessage);
            return Results.NoContent();
        }).WithName("DeleteAttachment");

        group.MapPost("/search", async ([FromBody] SearchRequest<AttachmentSearchFilter> request, [FromServices] IAttachmentService service, CancellationToken ct) =>
        {
            var response = await service.SearchAsync(request, ct);
            return Results.Ok(response);
        }).WithName("SearchAttachments");

        return app;
    }
}
