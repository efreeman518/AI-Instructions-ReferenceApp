using EF.AspNetCore;
using EF.Common.Contracts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using TaskFlow.Application.Contracts;
using EF.CQRS.Abstractions;
using TaskFlow.Application.Cqrs.Features.TaskItems;
using TaskFlow.Application.Models;

namespace TaskFlow.Api.Endpoints.Cqrs;

/// <summary>Maps task item CQRS HTTP routes to CQRS handlers and API contract metadata.</summary>
public static class TaskItemCqrsEndpoints
{
    private static bool _problemDetailsIncludeStackTrace;

    /// <summary>Registers task item CQRS routes, handlers, and response metadata.</summary>
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

        // Nested child routes. Comment, ChecklistItem, and the Tag association are internal to the
        // TaskItem aggregate, so they are mutated only through these sub-resource routes on the root
        // (GR-15) - there are no standalone /comments, /checklist-items, or /task-item-tags write
        // routes. Reads for comments/checklist-items still live on their own query endpoints.
        g.MapPost("/{id:guid}/comments", AddComment)
            .Produces<DefaultResponse<CommentDto>>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .WithSummary("Add a Comment to a TaskItem");

        g.MapPut("/{id:guid}/comments/{commentId:guid}", UpdateComment)
            .Produces<DefaultResponse<CommentDto>>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .WithSummary("Update a Comment on a TaskItem");

        g.MapDelete("/{id:guid}/comments/{commentId:guid}", RemoveComment)
            .Produces(StatusCodes.Status204NoContent)
            .ProducesValidationProblem()
            .WithSummary("Remove a Comment from a TaskItem");

        g.MapPost("/{id:guid}/checklist-items", AddChecklistItem)
            .Produces<DefaultResponse<ChecklistItemDto>>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .WithSummary("Add a ChecklistItem to a TaskItem");

        g.MapPut("/{id:guid}/checklist-items/{checklistItemId:guid}", UpdateChecklistItem)
            .Produces<DefaultResponse<ChecklistItemDto>>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .WithSummary("Update a ChecklistItem on a TaskItem");

        g.MapDelete("/{id:guid}/checklist-items/{checklistItemId:guid}", RemoveChecklistItem)
            .Produces(StatusCodes.Status204NoContent)
            .ProducesValidationProblem()
            .WithSummary("Remove a ChecklistItem from a TaskItem");

        g.MapPost("/{id:guid}/tags/{tagId:guid}", AssociateTag)
            .Produces<DefaultResponse<TaskItemTagDto>>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .WithSummary("Associate a Tag with a TaskItem");

        g.MapDelete("/{id:guid}/tags/{tagId:guid}", RemoveTag)
            .Produces(StatusCodes.Status204NoContent)
            .ProducesValidationProblem()
            .WithSummary("Remove a Tag association from a TaskItem");

        return group;
    }

    /// <summary>Handles search requests and returns a paged application response.</summary>
    private static async Task<IResult> Search(
        [FromServices] IRequestHandler<SearchTaskItemsQuery, PagedResponse<TaskItemDto>> handler,
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] SearchRequest<TaskItemSearchFilter>? request,
        CancellationToken ct)
    {
        var items = await handler.HandleAsync(new SearchTaskItemsQuery(request ?? new SearchRequest<TaskItemSearchFilter>()), ct);
        return TypedResults.Ok(items);
    }

    /// <summary>Loads requested data and maps missing records to the expected response.</summary>
    private static async Task<IResult> GetById(
        [FromServices] IRequestHandler<GetTaskItemByIdQuery, Result<DefaultResponse<TaskItemDto>>> handler,
        Guid id,
        CancellationToken ct)
    {
        var result = await handler.HandleAsync(new GetTaskItemByIdQuery(id), ct);
        return result.Match<IResult>(
            response => TypedResults.Ok(response),
            errors => TypedResults.Problem(ProblemDetailsHelper.BuildProblemDetailsResponseMultiple(
                errors: errors, statusCodeOverride: StatusCodes.Status400BadRequest)),
            () => TypedResults.NotFound(id));
    }

    /// <summary>Creates requested data after validation and maps the result to the caller contract.</summary>
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
                errors: errors, traceId: httpContext.TraceIdentifier,
                includeStackTrace: _problemDetailsIncludeStackTrace)));
    }

    /// <summary>Updates existing data after validation and preserves domain invariants.</summary>
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
                errors: errors, traceId: httpContext.TraceIdentifier,
                includeStackTrace: _problemDetailsIncludeStackTrace)));
    }

    /// <summary>Deletes requested data and maps failures to the caller contract.</summary>
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
                errors: errors, traceId: httpContext.TraceIdentifier,
                includeStackTrace: _problemDetailsIncludeStackTrace)));
    }

    /// <summary>Adds a comment to the TaskItem aggregate through the root.</summary>
    private static async Task<IResult> AddComment(
        HttpContext httpContext,
        [FromServices] IRequestHandler<AddTaskItemCommentCommand, Result<DefaultResponse<CommentDto>>> handler,
        Guid id,
        [FromBody] DefaultRequest<CommentDto> request,
        CancellationToken ct)
    {
        var result = await handler.HandleAsync(new AddTaskItemCommentCommand(id, request.Item), ct);
        return result.Match(
            response => response.Item is null
                ? Results.NotFound(id)
                : TypedResults.Created($"{httpContext.Request.Path}/{response.Item.Id}", response),
            errors => TypedResults.Problem(ProblemDetailsHelper.BuildProblemDetailsResponseMultiple(
                errors: errors, traceId: httpContext.TraceIdentifier,
                includeStackTrace: _problemDetailsIncludeStackTrace)));
    }

    /// <summary>Updates a comment owned by the TaskItem aggregate.</summary>
    private static async Task<IResult> UpdateComment(
        HttpContext httpContext,
        [FromServices] IRequestHandler<UpdateTaskItemCommentCommand, Result<DefaultResponse<CommentDto>>> handler,
        Guid id,
        Guid commentId,
        [FromBody] DefaultRequest<CommentDto> request,
        CancellationToken ct)
    {
        var result = await handler.HandleAsync(new UpdateTaskItemCommentCommand(id, commentId, request.Item), ct);
        return result.Match(
            response => response.Item is null ? Results.NotFound(commentId) : TypedResults.Ok(response),
            errors => TypedResults.Problem(ProblemDetailsHelper.BuildProblemDetailsResponseMultiple(
                errors: errors, traceId: httpContext.TraceIdentifier,
                includeStackTrace: _problemDetailsIncludeStackTrace)));
    }

    /// <summary>Removes a comment from the TaskItem aggregate through the root.</summary>
    private static async Task<IResult> RemoveComment(
        HttpContext httpContext,
        [FromServices] IRequestHandler<RemoveTaskItemCommentCommand, Result> handler,
        Guid id,
        Guid commentId,
        CancellationToken ct)
    {
        var result = await handler.HandleAsync(new RemoveTaskItemCommentCommand(id, commentId), ct);
        return result.Match<IResult>(
            () => TypedResults.NoContent(),
            errors => TypedResults.Problem(ProblemDetailsHelper.BuildProblemDetailsResponseMultiple(
                errors: errors, traceId: httpContext.TraceIdentifier,
                includeStackTrace: _problemDetailsIncludeStackTrace)));
    }

    /// <summary>Adds a checklist item to the TaskItem aggregate through the root.</summary>
    private static async Task<IResult> AddChecklistItem(
        HttpContext httpContext,
        [FromServices] IRequestHandler<AddTaskItemChecklistItemCommand, Result<DefaultResponse<ChecklistItemDto>>> handler,
        Guid id,
        [FromBody] DefaultRequest<ChecklistItemDto> request,
        CancellationToken ct)
    {
        var result = await handler.HandleAsync(new AddTaskItemChecklistItemCommand(id, request.Item), ct);
        return result.Match(
            response => response.Item is null
                ? Results.NotFound(id)
                : TypedResults.Created($"{httpContext.Request.Path}/{response.Item.Id}", response),
            errors => TypedResults.Problem(ProblemDetailsHelper.BuildProblemDetailsResponseMultiple(
                errors: errors, traceId: httpContext.TraceIdentifier,
                includeStackTrace: _problemDetailsIncludeStackTrace)));
    }

    /// <summary>Updates a checklist item owned by the TaskItem aggregate.</summary>
    private static async Task<IResult> UpdateChecklistItem(
        HttpContext httpContext,
        [FromServices] IRequestHandler<UpdateTaskItemChecklistItemCommand, Result<DefaultResponse<ChecklistItemDto>>> handler,
        Guid id,
        Guid checklistItemId,
        [FromBody] DefaultRequest<ChecklistItemDto> request,
        CancellationToken ct)
    {
        var result = await handler.HandleAsync(new UpdateTaskItemChecklistItemCommand(id, checklistItemId, request.Item), ct);
        return result.Match(
            response => response.Item is null ? Results.NotFound(checklistItemId) : TypedResults.Ok(response),
            errors => TypedResults.Problem(ProblemDetailsHelper.BuildProblemDetailsResponseMultiple(
                errors: errors, traceId: httpContext.TraceIdentifier,
                includeStackTrace: _problemDetailsIncludeStackTrace)));
    }

    /// <summary>Removes a checklist item from the TaskItem aggregate through the root.</summary>
    private static async Task<IResult> RemoveChecklistItem(
        HttpContext httpContext,
        [FromServices] IRequestHandler<RemoveTaskItemChecklistItemCommand, Result> handler,
        Guid id,
        Guid checklistItemId,
        CancellationToken ct)
    {
        var result = await handler.HandleAsync(new RemoveTaskItemChecklistItemCommand(id, checklistItemId), ct);
        return result.Match<IResult>(
            () => TypedResults.NoContent(),
            errors => TypedResults.Problem(ProblemDetailsHelper.BuildProblemDetailsResponseMultiple(
                errors: errors, traceId: httpContext.TraceIdentifier,
                includeStackTrace: _problemDetailsIncludeStackTrace)));
    }

    /// <summary>Associates an existing Tag with the TaskItem aggregate through the root.</summary>
    private static async Task<IResult> AssociateTag(
        HttpContext httpContext,
        [FromServices] IRequestHandler<AssociateTaskItemTagCommand, Result<DefaultResponse<TaskItemTagDto>>> handler,
        Guid id,
        Guid tagId,
        CancellationToken ct)
    {
        var result = await handler.HandleAsync(new AssociateTaskItemTagCommand(id, tagId), ct);
        return result.Match(
            response => response.Item is null
                ? Results.NotFound(id)
                : TypedResults.Created($"{httpContext.Request.Path}", response),
            errors => TypedResults.Problem(ProblemDetailsHelper.BuildProblemDetailsResponseMultiple(
                errors: errors, traceId: httpContext.TraceIdentifier,
                includeStackTrace: _problemDetailsIncludeStackTrace)));
    }

    /// <summary>Removes a Tag association from the TaskItem aggregate through the root.</summary>
    private static async Task<IResult> RemoveTag(
        HttpContext httpContext,
        [FromServices] IRequestHandler<RemoveTaskItemTagCommand, Result> handler,
        Guid id,
        Guid tagId,
        CancellationToken ct)
    {
        var result = await handler.HandleAsync(new RemoveTaskItemTagCommand(id, tagId), ct);
        return result.Match<IResult>(
            () => TypedResults.NoContent(),
            errors => TypedResults.Problem(ProblemDetailsHelper.BuildProblemDetailsResponseMultiple(
                errors: errors, traceId: httpContext.TraceIdentifier,
                includeStackTrace: _problemDetailsIncludeStackTrace)));
    }
}
