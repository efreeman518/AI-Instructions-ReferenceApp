using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using EF.AspNetCore;
using EF.Common.Contracts;
using TaskFlow.Application.Contracts;
using TaskFlow.Application.Contracts.Services;
using TaskFlow.Application.Models;

namespace TaskFlow.Api.Endpoints;

/// <summary>Maps task item HTTP routes to the selected application implementation and API contract metadata.</summary>
public static class TaskItemEndpoints
{
    private static bool _problemDetailsIncludeStackTrace;

    /// <summary>Registers task item routes, handlers, and response metadata.</summary>
    public static IEndpointRouteBuilder MapTaskItemEndpoints(this IEndpointRouteBuilder group, bool problemDetailsIncludeStackTrace)
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

        g.MapPatch("/{id:guid}", Patch)
            .Produces<DefaultResponse<TaskItemDto>>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .WithSummary("Partially update a TaskItem (JSON merge patch - omitted fields are unchanged)");

        g.MapDelete("/{id:guid}", Delete)
            .Produces(StatusCodes.Status204NoContent)
            .ProducesValidationProblem()
            .WithSummary("Delete a TaskItem");

        // Nested child routes - Comment, ChecklistItem, and the Tag association are internal to the
        // TaskItem aggregate and mutated only through the root (GR-15). No standalone child write routes.
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
        [FromServices] ITaskItemService service,
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] SearchRequest<TaskItemSearchFilter>? request,
        CancellationToken ct)
    {
        var items = await service.SearchAsync(request ?? new SearchRequest<TaskItemSearchFilter>(), ct);
        return TypedResults.Ok(items);
    }

    /// <summary>Loads requested data and maps missing records to the expected response.</summary>
    private static async Task<IResult> GetById(
        [FromServices] ITaskItemService service, Guid id, CancellationToken ct)
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
        [FromServices] ITaskItemService service,
        [FromBody] DefaultRequest<TaskItemDto> request,
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
        [FromServices] ITaskItemService service,
        Guid id,
        [FromBody] DefaultRequest<TaskItemDto> request,
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

    /// <summary>Applies a sparse partial update (JSON merge patch) to a TaskItem through the aggregate root.</summary>
    private static async Task<IResult> Patch(
        HttpContext httpContext,
        [FromServices] ITaskItemService service,
        Guid id,
        [FromBody] DefaultRequest<TaskItemPatchDto> request,
        CancellationToken ct)
    {
        var result = await service.PatchAsync(id, request.Item, ct);
        return result.Match(
            response => response.Item is null ? Results.NotFound(id) : TypedResults.Ok(response),
            errors => TypedResults.Problem(ProblemDetailsHelper.BuildProblemDetailsResponseMultiple(
                messages: errors, traceId: httpContext.TraceIdentifier,
                includeStackTrace: _problemDetailsIncludeStackTrace)));
    }

    /// <summary>Deletes requested data and maps failures to the caller contract.</summary>
    private static async Task<IResult> Delete(
        HttpContext httpContext,
        [FromServices] ITaskItemService service, Guid id, CancellationToken ct)
    {
        var result = await service.DeleteAsync(id, ct);
        return result.Match<IResult>(
            () => TypedResults.NoContent(),
            errors => TypedResults.Problem(
                ProblemDetailsHelper.BuildProblemDetailsResponseMultiple(
                    messages: errors, traceId: httpContext.TraceIdentifier,
                    includeStackTrace: _problemDetailsIncludeStackTrace)));
    }

    /// <summary>Adds a comment to the TaskItem aggregate through the root.</summary>
    private static async Task<IResult> AddComment(
        HttpContext httpContext,
        [FromServices] ITaskItemService service,
        Guid id,
        [FromBody] DefaultRequest<CommentDto> request,
        CancellationToken ct)
    {
        var result = await service.AddCommentAsync(id, request.Item, ct);
        return result.Match(
            response => response.Item is null
                ? Results.NotFound(id)
                : TypedResults.Created($"{httpContext.Request.Path}/{response.Item.Id}", response),
            errors => TypedResults.Problem(ProblemDetailsHelper.BuildProblemDetailsResponseMultiple(
                messages: errors, traceId: httpContext.TraceIdentifier,
                includeStackTrace: _problemDetailsIncludeStackTrace)));
    }

    /// <summary>Updates a comment owned by the TaskItem aggregate.</summary>
    private static async Task<IResult> UpdateComment(
        HttpContext httpContext,
        [FromServices] ITaskItemService service,
        Guid id,
        Guid commentId,
        [FromBody] DefaultRequest<CommentDto> request,
        CancellationToken ct)
    {
        var result = await service.UpdateCommentAsync(id, commentId, request.Item, ct);
        return result.Match(
            response => response.Item is null ? Results.NotFound(commentId) : TypedResults.Ok(response),
            errors => TypedResults.Problem(ProblemDetailsHelper.BuildProblemDetailsResponseMultiple(
                messages: errors, traceId: httpContext.TraceIdentifier,
                includeStackTrace: _problemDetailsIncludeStackTrace)));
    }

    /// <summary>Removes a comment from the TaskItem aggregate through the root.</summary>
    private static async Task<IResult> RemoveComment(
        HttpContext httpContext,
        [FromServices] ITaskItemService service,
        Guid id,
        Guid commentId,
        CancellationToken ct)
    {
        var result = await service.RemoveCommentAsync(id, commentId, ct);
        return result.Match<IResult>(
            () => TypedResults.NoContent(),
            errors => TypedResults.Problem(ProblemDetailsHelper.BuildProblemDetailsResponseMultiple(
                messages: errors, traceId: httpContext.TraceIdentifier,
                includeStackTrace: _problemDetailsIncludeStackTrace)));
    }

    /// <summary>Adds a checklist item to the TaskItem aggregate through the root.</summary>
    private static async Task<IResult> AddChecklistItem(
        HttpContext httpContext,
        [FromServices] ITaskItemService service,
        Guid id,
        [FromBody] DefaultRequest<ChecklistItemDto> request,
        CancellationToken ct)
    {
        var result = await service.AddChecklistItemAsync(id, request.Item, ct);
        return result.Match(
            response => response.Item is null
                ? Results.NotFound(id)
                : TypedResults.Created($"{httpContext.Request.Path}/{response.Item.Id}", response),
            errors => TypedResults.Problem(ProblemDetailsHelper.BuildProblemDetailsResponseMultiple(
                messages: errors, traceId: httpContext.TraceIdentifier,
                includeStackTrace: _problemDetailsIncludeStackTrace)));
    }

    /// <summary>Updates a checklist item owned by the TaskItem aggregate.</summary>
    private static async Task<IResult> UpdateChecklistItem(
        HttpContext httpContext,
        [FromServices] ITaskItemService service,
        Guid id,
        Guid checklistItemId,
        [FromBody] DefaultRequest<ChecklistItemDto> request,
        CancellationToken ct)
    {
        var result = await service.UpdateChecklistItemAsync(id, checklistItemId, request.Item, ct);
        return result.Match(
            response => response.Item is null ? Results.NotFound(checklistItemId) : TypedResults.Ok(response),
            errors => TypedResults.Problem(ProblemDetailsHelper.BuildProblemDetailsResponseMultiple(
                messages: errors, traceId: httpContext.TraceIdentifier,
                includeStackTrace: _problemDetailsIncludeStackTrace)));
    }

    /// <summary>Removes a checklist item from the TaskItem aggregate through the root.</summary>
    private static async Task<IResult> RemoveChecklistItem(
        HttpContext httpContext,
        [FromServices] ITaskItemService service,
        Guid id,
        Guid checklistItemId,
        CancellationToken ct)
    {
        var result = await service.RemoveChecklistItemAsync(id, checklistItemId, ct);
        return result.Match<IResult>(
            () => TypedResults.NoContent(),
            errors => TypedResults.Problem(ProblemDetailsHelper.BuildProblemDetailsResponseMultiple(
                messages: errors, traceId: httpContext.TraceIdentifier,
                includeStackTrace: _problemDetailsIncludeStackTrace)));
    }

    /// <summary>Associates an existing Tag with the TaskItem aggregate through the root.</summary>
    private static async Task<IResult> AssociateTag(
        HttpContext httpContext,
        [FromServices] ITaskItemService service,
        Guid id,
        Guid tagId,
        CancellationToken ct)
    {
        var result = await service.AssociateTagAsync(id, tagId, ct);
        return result.Match(
            response => response.Item is null
                ? Results.NotFound(id)
                : TypedResults.Created($"{httpContext.Request.Path}", response),
            errors => TypedResults.Problem(ProblemDetailsHelper.BuildProblemDetailsResponseMultiple(
                messages: errors, traceId: httpContext.TraceIdentifier,
                includeStackTrace: _problemDetailsIncludeStackTrace)));
    }

    /// <summary>Removes a Tag association from the TaskItem aggregate through the root.</summary>
    private static async Task<IResult> RemoveTag(
        HttpContext httpContext,
        [FromServices] ITaskItemService service,
        Guid id,
        Guid tagId,
        CancellationToken ct)
    {
        var result = await service.RemoveTagAsync(id, tagId, ct);
        return result.Match<IResult>(
            () => TypedResults.NoContent(),
            errors => TypedResults.Problem(ProblemDetailsHelper.BuildProblemDetailsResponseMultiple(
                messages: errors, traceId: httpContext.TraceIdentifier,
                includeStackTrace: _problemDetailsIncludeStackTrace)));
    }
}
