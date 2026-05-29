using EF.AspNetCore;
using EF.Common.Contracts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using TaskFlow.Application.Contracts;
using EF.CQRS.Abstractions;
using TaskFlow.Application.Cqrs.Features.ChecklistItems;
using TaskFlow.Application.Models;

namespace TaskFlow.Api.Endpoints.Cqrs;

/// <summary>Maps checklist item CQRS HTTP routes to CQRS handlers and API contract metadata.</summary>
public static class ChecklistItemCqrsEndpoints
{
    private static bool _problemDetailsIncludeStackTrace;

    /// <summary>Registers checklist item CQRS routes, handlers, and response metadata.</summary>
    public static IEndpointRouteBuilder MapChecklistItemCqrsEndpoints(this IEndpointRouteBuilder group, bool problemDetailsIncludeStackTrace)
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
        [FromServices] IRequestHandler<SearchChecklistItemsQuery, PagedResponse<ChecklistItemDto>> handler,
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] SearchRequest<ChecklistItemSearchFilter>? request,
        CancellationToken ct)
    {
        var items = await handler.HandleAsync(new SearchChecklistItemsQuery(request ?? new SearchRequest<ChecklistItemSearchFilter>()), ct);
        return TypedResults.Ok(items);
    }

    /// <summary>Loads requested data and maps missing records to the expected response.</summary>
    private static async Task<IResult> GetById(
        [FromServices] IRequestHandler<GetChecklistItemByIdQuery, Result<DefaultResponse<ChecklistItemDto>>> handler,
        Guid id,
        CancellationToken ct)
    {
        var result = await handler.HandleAsync(new GetChecklistItemByIdQuery(id), ct);
        return result.Match<IResult>(
            response => TypedResults.Ok(response),
            errors => TypedResults.Problem(ProblemDetailsHelper.BuildProblemDetailsResponseMultiple(
                messages: errors, statusCodeOverride: StatusCodes.Status400BadRequest)),
            () => TypedResults.NotFound(id));
    }

    /// <summary>Creates requested data after validation and maps the result to the caller contract.</summary>
    private static async Task<IResult> Create(
        HttpContext httpContext,
        [FromServices] IRequestHandler<CreateChecklistItemCommand, Result<DefaultResponse<ChecklistItemDto>>> handler,
        [FromBody] DefaultRequest<ChecklistItemDto> request,
        CancellationToken ct)
    {
        var result = await handler.HandleAsync(new CreateChecklistItemCommand(request), ct);
        return result.Match<IResult>(
            response => TypedResults.Created(httpContext.Request.Path, response),
            errors => TypedResults.Problem(ProblemDetailsHelper.BuildProblemDetailsResponseMultiple(
                messages: errors, traceId: httpContext.TraceIdentifier,
                includeStackTrace: _problemDetailsIncludeStackTrace)));
    }

    /// <summary>Updates existing data after validation and preserves domain invariants.</summary>
    private static async Task<IResult> Update(
        HttpContext httpContext,
        [FromServices] IRequestHandler<UpdateChecklistItemCommand, Result<DefaultResponse<ChecklistItemDto>>> handler,
        Guid id,
        [FromBody] DefaultRequest<ChecklistItemDto> request,
        CancellationToken ct)
    {
        if (request.Item.Id != null && request.Item.Id != id)
            return TypedResults.Problem(ProblemDetailsHelper.BuildProblemDetailsResponse(
                statusCodeOverride: StatusCodes.Status400BadRequest,
                message: $"{ErrorConstants.ERROR_URL_BODY_ID_MISMATCH}: {id} <> {request.Item.Id}"));

        var result = await handler.HandleAsync(new UpdateChecklistItemCommand(request), ct);
        return result.Match(
            response => response.Item is null ? Results.NotFound(id) : TypedResults.Ok(response),
            errors => TypedResults.Problem(ProblemDetailsHelper.BuildProblemDetailsResponseMultiple(
                messages: errors, traceId: httpContext.TraceIdentifier,
                includeStackTrace: _problemDetailsIncludeStackTrace)));
    }

    /// <summary>Deletes requested data and maps failures to the caller contract.</summary>
    private static async Task<IResult> Delete(
        HttpContext httpContext,
        [FromServices] IRequestHandler<DeleteChecklistItemCommand, Result> handler,
        Guid id,
        CancellationToken ct)
    {
        var result = await handler.HandleAsync(new DeleteChecklistItemCommand(id), ct);
        return result.Match<IResult>(
            () => TypedResults.NoContent(),
            errors => TypedResults.Problem(ProblemDetailsHelper.BuildProblemDetailsResponseMultiple(
                messages: errors, traceId: httpContext.TraceIdentifier,
                includeStackTrace: _problemDetailsIncludeStackTrace)));
    }
}
