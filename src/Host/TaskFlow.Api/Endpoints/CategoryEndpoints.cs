using Microsoft.AspNetCore.Mvc;
using EF.Common.Contracts;
using TaskFlow.Application.Contracts.Services;
using TaskFlow.Application.Models;

namespace TaskFlow.Api.Endpoints;

public static class CategoryEndpoints
{
    public static IEndpointRouteBuilder MapCategoryEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/categories").WithTags("Categories");

        group.MapGet("/{id:guid}", async (Guid id, [FromServices] ICategoryService service, CancellationToken ct) =>
        {
            var result = await service.GetAsync(id, ct);
            if (result.IsNone) return Results.NotFound();
            if (result.IsFailure) return Results.Problem(result.ErrorMessage);
            return Results.Ok(result.Value!.Item);
        }).WithName("GetCategory");

        group.MapPost("/", async ([FromBody] CategoryDto dto, [FromServices] ICategoryService service, CancellationToken ct) =>
        {
            var result = await service.CreateAsync(new DefaultRequest<CategoryDto> { Item = dto }, ct);
            if (result.IsFailure) return Results.BadRequest(result.ErrorMessage);
            return Results.Created($"/api/categories/{result.Value!.Item!.Id}", result.Value.Item);
        }).WithName("CreateCategory");

        group.MapPut("/{id:guid}", async (Guid id, [FromBody] CategoryDto dto, [FromServices] ICategoryService service, CancellationToken ct) =>
        {
            dto.Id = id;
            var result = await service.UpdateAsync(new DefaultRequest<CategoryDto> { Item = dto }, ct);
            if (result.IsFailure) return Results.BadRequest(result.ErrorMessage);
            if (result.Value?.Item == null) return Results.NotFound();
            return Results.Ok(result.Value.Item);
        }).WithName("UpdateCategory");

        group.MapDelete("/{id:guid}", async (Guid id, [FromServices] ICategoryService service, CancellationToken ct) =>
        {
            var result = await service.DeleteAsync(id, ct);
            if (result.IsFailure) return Results.Problem(result.ErrorMessage);
            return Results.NoContent();
        }).WithName("DeleteCategory");

        group.MapPost("/search", async ([FromBody] SearchRequest<CategorySearchFilter> request, [FromServices] ICategoryService service, CancellationToken ct) =>
        {
            var response = await service.SearchAsync(request, ct);
            return Results.Ok(response);
        }).WithName("SearchCategories");

        return app;
    }
}
