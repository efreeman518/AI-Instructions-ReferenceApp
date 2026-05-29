using Microsoft.AspNetCore.Mvc;
using EF.AspNetCore;
using EF.Common.Contracts;
using TaskFlow.Application.Contracts.Services;
using TaskFlow.Application.Models;

namespace TaskFlow.Api.Endpoints;

/// <summary>Maps task item tag HTTP routes to the selected application implementation and API contract metadata.</summary>
public static class TaskItemTagEndpoints
{
    private static bool _problemDetailsIncludeStackTrace;

    /// <summary>Registers task item tag routes, handlers, and response metadata.</summary>
    public static IEndpointRouteBuilder MapTaskItemTagEndpoints(this IEndpointRouteBuilder group, bool problemDetailsIncludeStackTrace)
    {
        _problemDetailsIncludeStackTrace = problemDetailsIncludeStackTrace;

        var g = group.MapGroup("/task-item-tags").WithTags("TaskItemTags");

        g.MapGet("/{id:guid}", GetById)
            .Produces<DefaultResponse<TaskItemTagDto>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .WithSummary("Get a single TaskItemTag");

        g.MapPost("/", Create)
            .Produces<DefaultResponse<TaskItemTagDto>>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .WithSummary("Create a new TaskItemTag");

        g.MapDelete("/{id:guid}", Delete)
            .Produces(StatusCodes.Status204NoContent)
            .ProducesValidationProblem()
            .WithSummary("Delete a TaskItemTag");

        return group;
    }

    /// <summary>Loads requested data and maps missing records to the expected response.</summary>
    private static async Task<IResult> GetById(
        [FromServices] ITaskItemTagService service, Guid id, CancellationToken ct)
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
        [FromServices] ITaskItemTagService service,
        [FromBody] DefaultRequest<TaskItemTagDto> request,
        CancellationToken ct)
    {
        var result = await service.CreateAsync(request, ct);
        return result.Match<IResult>(
            response => TypedResults.Created(httpContext.Request.Path, response),
            errors => TypedResults.Problem(ProblemDetailsHelper.BuildProblemDetailsResponseMultiple(
                messages: errors, traceId: httpContext.TraceIdentifier,
                includeStackTrace: _problemDetailsIncludeStackTrace)));
    }

    /// <summary>Deletes requested data and maps failures to the caller contract.</summary>
    private static async Task<IResult> Delete(
        HttpContext httpContext,
        [FromServices] ITaskItemTagService service, Guid id, CancellationToken ct)
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
