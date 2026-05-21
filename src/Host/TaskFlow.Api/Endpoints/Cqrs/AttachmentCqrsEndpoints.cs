using EF.AspNetCore;
using EF.Common.Contracts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using TaskFlow.Application.Contracts;
using EF.CQRS.Abstractions;
using TaskFlow.Application.Cqrs.Requests;
using TaskFlow.Application.Models;
using TaskFlow.Domain.Shared.Enums;

namespace TaskFlow.Api.Endpoints.Cqrs;

public static class AttachmentCqrsEndpoints
{
    private static bool _problemDetailsIncludeStackTrace;

    public static IEndpointRouteBuilder MapAttachmentCqrsEndpoints(this IEndpointRouteBuilder group, bool problemDetailsIncludeStackTrace)
    {
        _problemDetailsIncludeStackTrace = problemDetailsIncludeStackTrace;

        var g = group.MapGroup("/attachments").WithTags("Attachments");

        g.MapPost("/search", Search)
            .Produces<PagedResponse<AttachmentDto>>(StatusCodes.Status200OK)
            .WithSummary("Search Attachments with paging, filters, and sorts");

        g.MapGet("/{id:guid}", GetById)
            .Produces<DefaultResponse<AttachmentDto>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .WithSummary("Get a single Attachment");

        g.MapPost("/", Create)
            .Produces<DefaultResponse<AttachmentDto>>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .WithSummary("Create a new Attachment");

        g.MapPost("/upload", Upload)
            .Produces<DefaultResponse<AttachmentDto>>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .WithSummary("Upload a file Attachment")
            .DisableAntiforgery();

        g.MapPut("/{id:guid}", Update)
            .Produces<DefaultResponse<AttachmentDto>>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .WithSummary("Update an existing Attachment");

        g.MapDelete("/{id:guid}", Delete)
            .Produces(StatusCodes.Status204NoContent)
            .ProducesValidationProblem()
            .WithSummary("Delete an Attachment");

        return group;
    }

    private static async Task<IResult> Search(
        [FromServices] IRequestHandler<SearchAttachmentsQuery, PagedResponse<AttachmentDto>> handler,
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] SearchRequest<AttachmentSearchFilter>? request,
        CancellationToken ct)
    {
        var items = await handler.HandleAsync(new SearchAttachmentsQuery(request ?? new SearchRequest<AttachmentSearchFilter>()), ct);
        return TypedResults.Ok(items);
    }

    private static async Task<IResult> GetById(
        [FromServices] IRequestHandler<GetAttachmentByIdQuery, Result<DefaultResponse<AttachmentDto>>> handler,
        Guid id,
        CancellationToken ct)
    {
        var result = await handler.HandleAsync(new GetAttachmentByIdQuery(id), ct);
        return result.Match<IResult>(
            response => TypedResults.Ok(response),
            errors => TypedResults.Problem(ProblemDetailsHelper.BuildProblemDetailsResponseMultiple(
                messages: errors, statusCodeOverride: StatusCodes.Status400BadRequest)),
            () => TypedResults.NotFound(id));
    }

    private static async Task<IResult> Create(
        HttpContext httpContext,
        [FromServices] IRequestHandler<CreateAttachmentCommand, Result<DefaultResponse<AttachmentDto>>> handler,
        [FromBody] DefaultRequest<AttachmentDto> request,
        CancellationToken ct)
    {
        var result = await handler.HandleAsync(new CreateAttachmentCommand(request), ct);
        return result.Match<IResult>(
            response => TypedResults.Created(httpContext.Request.Path, response),
            errors => TypedResults.Problem(ProblemDetailsHelper.BuildProblemDetailsResponseMultiple(
                messages: errors, traceId: httpContext.TraceIdentifier,
                includeStackTrace: _problemDetailsIncludeStackTrace)));
    }

    private static async Task<IResult> Upload(
        HttpContext httpContext,
        IFormFile file,
        [FromForm] AttachmentOwnerType ownerType,
        [FromForm] Guid ownerId,
        [FromServices] IRequestHandler<UploadAttachmentCommand, Result<DefaultResponse<AttachmentDto>>> handler,
        CancellationToken ct)
    {
        await using var stream = file.OpenReadStream();
        var result = await handler.HandleAsync(
            new UploadAttachmentCommand(stream, file.FileName, file.ContentType, file.Length, ownerType, ownerId),
            ct);
        return result.Match<IResult>(
            response => TypedResults.Created(httpContext.Request.Path, response),
            errors => TypedResults.Problem(ProblemDetailsHelper.BuildProblemDetailsResponseMultiple(
                messages: errors, traceId: httpContext.TraceIdentifier,
                includeStackTrace: _problemDetailsIncludeStackTrace)));
    }

    private static async Task<IResult> Update(
        HttpContext httpContext,
        [FromServices] IRequestHandler<UpdateAttachmentCommand, Result<DefaultResponse<AttachmentDto>>> handler,
        Guid id,
        [FromBody] DefaultRequest<AttachmentDto> request,
        CancellationToken ct)
    {
        if (request.Item.Id != null && request.Item.Id != id)
            return TypedResults.Problem(ProblemDetailsHelper.BuildProblemDetailsResponse(
                statusCodeOverride: StatusCodes.Status400BadRequest,
                message: $"{ErrorConstants.ERROR_URL_BODY_ID_MISMATCH}: {id} <> {request.Item.Id}"));

        var result = await handler.HandleAsync(new UpdateAttachmentCommand(request), ct);
        return result.Match(
            response => response.Item is null ? Results.NotFound(id) : TypedResults.Ok(response),
            errors => TypedResults.Problem(ProblemDetailsHelper.BuildProblemDetailsResponseMultiple(
                messages: errors, traceId: httpContext.TraceIdentifier,
                includeStackTrace: _problemDetailsIncludeStackTrace)));
    }

    private static async Task<IResult> Delete(
        HttpContext httpContext,
        [FromServices] IRequestHandler<DeleteAttachmentCommand, Result> handler,
        Guid id,
        CancellationToken ct)
    {
        var result = await handler.HandleAsync(new DeleteAttachmentCommand(id), ct);
        return result.Match<IResult>(
            () => TypedResults.NoContent(),
            errors => TypedResults.Problem(ProblemDetailsHelper.BuildProblemDetailsResponseMultiple(
                messages: errors, traceId: httpContext.TraceIdentifier,
                includeStackTrace: _problemDetailsIncludeStackTrace)));
    }
}
