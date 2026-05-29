using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using EF.AspNetCore;
using EF.Common.Contracts;
using TaskFlow.Application.Contracts;
using TaskFlow.Application.Contracts.Services;
using TaskFlow.Application.Models;

namespace TaskFlow.Api.Endpoints;

/// <summary>Maps checklist item HTTP routes to the selected application implementation and API contract metadata.</summary>
public static class ChecklistItemEndpoints
{
    private static bool _problemDetailsIncludeStackTrace;

    /// <summary>Registers checklist item routes, handlers, and response metadata.</summary>
    public static IEndpointRouteBuilder MapChecklistItemEndpoints(this IEndpointRouteBuilder group, bool problemDetailsIncludeStackTrace)
    {
        _problemDetailsIncludeStackTrace = problemDetailsIncludeStackTrace;

        var g = group.MapGroup("/checklist-items").WithTags("ChecklistItems");

        g.MapPost("/search", Search)
            .Produces<PagedResponse<ChecklistItemDto>>(StatusCodes.Status200OK)
            .WithSummary("Search ChecklistItems with paging, filters, and sorts");

        g.MapGet("/{id:guid}", GetById)
            .Produces<DefaultResponse<ChecklistItemDto>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .WithSummary("Get a single ChecklistItem");

        g.MapPost("/", Create)
            .Produces<DefaultResponse<ChecklistItemDto>>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .WithSummary("Create a new ChecklistItem");

        g.MapPut("/{id:guid}", Update)
            .Produces<DefaultResponse<ChecklistItemDto>>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .WithSummary("Update an existing ChecklistItem");

        g.MapDelete("/{id:guid}", Delete)
            .Produces(StatusCodes.Status204NoContent)
            .ProducesValidationProblem()
            .WithSummary("Delete a ChecklistItem");

        return group;
    }

    /// <summary>Handles search requests and returns a paged application response.</summary>
    private static async Task<IResult> Search(
        [FromServices] IChecklistItemService service,
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] SearchRequest<ChecklistItemSearchFilter>? request,
        CancellationToken ct)
    {
        var items = await service.SearchAsync(request ?? new SearchRequest<ChecklistItemSearchFilter>(), ct);
        return TypedResults.Ok(items);
    }

    /// <summary>Loads requested data and maps missing records to the expected response.</summary>
    private static async Task<IResult> GetById(
        [FromServices] IChecklistItemService service, Guid id, CancellationToken ct)
    {
        var result = await service.GetAsync(id, ct);
        return result.Match<IResult>(
            response => TypedResults.Ok(response),
            errors => TypedResults.Problem(ProblemDetailsHelper.BuildProblemDetailsResponseMultiple(
                messages: errors, statusCodeOverride: StatusCodes.Status400BadRequest)),
            () => TypedResults.NotFound(id));
    }

    /// <summary>Creates requested data after validation and maps the result to the caller contract.</summary>
    private static async Task<IResult> Create(
        HttpContext httpContext,
        [FromServices] IChecklistItemService service,
        [FromBody] DefaultRequest<ChecklistItemDto> request,
        CancellationToken ct)
    {
        var result = await service.CreateAsync(request, ct);
        return result.Match<IResult>(
            response => TypedResults.Created(httpContext.Request.Path, response),
            errors => TypedResults.Problem(ProblemDetailsHelper.BuildProblemDetailsResponseMultiple(
                messages: errors, traceId: httpContext.TraceIdentifier,
                includeStackTrace: _problemDetailsIncludeStackTrace)));
    }

    /// <summary>Updates existing data after validation and preserves domain invariants.</summary>
    private static async Task<IResult> Update(
        HttpContext httpContext,
        [FromServices] IChecklistItemService service,
        Guid id,
        [FromBody] DefaultRequest<ChecklistItemDto> request,
        CancellationToken ct)
    {
        if (request.Item.Id != null && request.Item.Id != id)
            return TypedResults.Problem(ProblemDetailsHelper.BuildProblemDetailsResponse(
                statusCodeOverride: StatusCodes.Status400BadRequest,
                message: $"{ErrorConstants.ERROR_URL_BODY_ID_MISMATCH}: {id} <> {request.Item.Id}"));

        var result = await service.UpdateAsync(request, ct);
        return result.Match(
            response => response.Item is null ? Results.NotFound(id) : TypedResults.Ok(response),
            errors => TypedResults.Problem(ProblemDetailsHelper.BuildProblemDetailsResponseMultiple(
                messages: errors, traceId: httpContext.TraceIdentifier,
                includeStackTrace: _problemDetailsIncludeStackTrace)));
    }

    /// <summary>Deletes requested data and maps failures to the caller contract.</summary>
    private static async Task<IResult> Delete(
        HttpContext httpContext,
        [FromServices] IChecklistItemService service, Guid id, CancellationToken ct)
    {
        var result = await service.DeleteAsync(id, ct);
        return result.Match<IResult>(
            () => TypedResults.NoContent(),
            errors => TypedResults.Problem(
                ProblemDetailsHelper.BuildProblemDetailsResponseMultiple(
                    messages: errors, traceId: httpContext.TraceIdentifier,
                    includeStackTrace: _problemDetailsIncludeStackTrace)));
    }
}
