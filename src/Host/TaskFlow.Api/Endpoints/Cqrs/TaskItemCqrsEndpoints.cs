using EF.AspNetCore;
using EF.Common.Contracts;
using EF.CQRS.Abstractions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using TaskFlow.Application.Contracts;
using TaskFlow.Application.Cqrs.Requests;
using TaskFlow.Application.Models;

namespace TaskFlow.Api.Endpoints.Cqrs;

public static class TaskItemCqrsEndpoints
{
    private static bool _problemDetailsIncludeStackTrace;

    public static IEndpointRouteBuilder MapTaskItemCqrsEndpoints(this IEndpointRouteBuilder group, bool problemDetailsIncludeStackTrace)
    {
        _problemDetailsIncludeStackTrace = problemDetailsIncludeStackTrace;

        var g = group.MapGroup("/task-items").WithTags("TaskItems");

        g.MapPost("/search", Search)
            .Produces<PagedResponse<TaskItemDto>>(StatusCodes.Status200OK)
            .WithSummary("Search TaskItems with paging, filters, and sorts");

        g.MapGet("/{id:guid}", GetById)
            .Produces<DefaultResponse<TaskItemDto>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .WithSummary("Get a single TaskItem");

        g.MapPost("/", Create)
            .Produces<DefaultResponse<TaskItemDto>>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .WithSummary("Create a new TaskItem");

        g.MapPut("/{id:guid}", Update)
            .Produces<DefaultResponse<TaskItemDto>>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .WithSummary("Update an existing TaskItem");

        g.MapDelete("/{id:guid}", Delete)
            .Produces(StatusCodes.Status204NoContent)
            .ProducesValidationProblem()
            .WithSummary("Delete a TaskItem");

        return group;
    }

    private static async Task<IResult> Search(
        [FromServices] IRequestHandler<SearchTaskItemsQuery, PagedResponse<TaskItemDto>> handler,
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] SearchRequest<TaskItemSearchFilter>? request,
        CancellationToken ct)
    {
        var items = await handler.HandleAsync(new SearchTaskItemsQuery(request ?? new SearchRequest<TaskItemSearchFilter>()), ct);
        return TypedResults.Ok(items);
    }

    private static async Task<IResult> GetById(
        [FromServices] IRequestHandler<GetTaskItemByIdQuery, Result<DefaultResponse<TaskItemDto>>> handler,
        Guid id,
        CancellationToken ct)
    {
        var result = await handler.HandleAsync(new GetTaskItemByIdQuery(id), ct);
        return result.Match<IResult>(
            response => TypedResults.Ok(response),
            errors => TypedResults.Problem(ProblemDetailsHelper.BuildProblemDetailsResponseMultiple(
                messages: errors, statusCodeOverride: StatusCodes.Status400BadRequest)),
            () => TypedResults.NotFound(id));
    }

    private static async Task<IResult> Create(
        HttpContext httpContext,
        [FromServices] IRequestHandler<CreateTaskItemCommand, Result<DefaultResponse<TaskItemDto>>> handler,
        [FromBody] DefaultRequest<TaskItemDto> request,
        CancellationToken ct)
    {
        var result = await handler.HandleAsync(new CreateTaskItemCommand(request), ct);
        return result.Match<IResult>(
            response => TypedResults.Created(httpContext.Request.Path, response),
            errors => TypedResults.Problem(ProblemDetailsHelper.BuildProblemDetailsResponseMultiple(
                messages: errors, traceId: httpContext.TraceIdentifier,
                includeStackTrace: _problemDetailsIncludeStackTrace)));
    }

    private static async Task<IResult> Update(
        HttpContext httpContext,
        [FromServices] IRequestHandler<UpdateTaskItemCommand, Result<DefaultResponse<TaskItemDto>>> handler,
        Guid id,
        [FromBody] DefaultRequest<TaskItemDto> request,
        CancellationToken ct)
    {
        if (request.Item.Id != null && request.Item.Id != id)
            return TypedResults.Problem(ProblemDetailsHelper.BuildProblemDetailsResponse(
                statusCodeOverride: StatusCodes.Status400BadRequest,
                message: $"{ErrorConstants.ERROR_URL_BODY_ID_MISMATCH}: {id} <> {request.Item.Id}"));

        var result = await handler.HandleAsync(new UpdateTaskItemCommand(request), ct);
        return result.Match(
            response => response.Item is null ? Results.NotFound(id) : TypedResults.Ok(response),
            errors => TypedResults.Problem(ProblemDetailsHelper.BuildProblemDetailsResponseMultiple(
                messages: errors, traceId: httpContext.TraceIdentifier,
                includeStackTrace: _problemDetailsIncludeStackTrace)));
    }

    private static async Task<IResult> Delete(
        HttpContext httpContext,
        [FromServices] IRequestHandler<DeleteTaskItemCommand, Result> handler,
        Guid id,
        CancellationToken ct)
    {
        var result = await handler.HandleAsync(new DeleteTaskItemCommand(id), ct);
        return result.Match<IResult>(
            () => TypedResults.NoContent(),
            errors => TypedResults.Problem(ProblemDetailsHelper.BuildProblemDetailsResponseMultiple(
                messages: errors, traceId: httpContext.TraceIdentifier,
                includeStackTrace: _problemDetailsIncludeStackTrace)));
    }
}
