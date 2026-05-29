using EF.Common.Contracts;
using TaskFlow.Application.Cqrs.Shared;
using EF.Data.Contracts;
using Microsoft.Extensions.Logging;
using TaskFlow.Application.Contracts;
using TaskFlow.Application.Contracts.Repositories;
using EF.CQRS.Abstractions;
using TaskFlow.Application.Mappers;
using TaskFlow.Application.Models;
using TaskFlow.Domain.Model;

namespace TaskFlow.Application.Cqrs.Features.Comments;

/// <summary>Handles search comments work by coordinating validation, tenant boundaries, persistence, and response mapping.</summary>
internal sealed class SearchCommentsHandler(
    ILogger<SearchCommentsHandler> logger,
    IRequestContext<string, Guid?> requestContext,
    ICommentRepositoryQuery repoQuery)
    : IRequestHandler<SearchCommentsQuery, PagedResponse<CommentDto>>
{
    /// <summary>Handles search comments requests and returns the application result.</summary>
    public async Task<PagedResponse<CommentDto>> HandleAsync(SearchCommentsQuery query, CancellationToken ct = default)
    {
        var request = query.Request;
        HandlerHelpers.EnforceTenantFilter(request, requestContext.TenantId, requestContext.Roles, logger, "CommentSearch");
        return await CqrsHandlerSupport.SearchAsync(token => repoQuery.SearchCommentsAsync(request, token), logger, "Comment", ct);
    }
}

/// <summary>Handles get comment by ID work by coordinating validation, tenant boundaries, persistence, and response mapping.</summary>
internal sealed class GetCommentByIdHandler(
    ILogger<GetCommentByIdHandler> logger,
    IRequestContext<string, Guid?> requestContext,
    ICommentRepositoryQuery repoQuery,
    ITenantBoundaryValidator tenantBoundaryValidator)
    : IRequestHandler<GetCommentByIdQuery, Result<DefaultResponse<CommentDto>>>
{
    /// <summary>Handles get comment by ID requests and returns the application result.</summary>
    public async Task<Result<DefaultResponse<CommentDto>>> HandleAsync(GetCommentByIdQuery query, CancellationToken ct = default)
    {
        var entity = await repoQuery.GetCommentAsync(query.Id, ct);
        if (entity is null) return Result<DefaultResponse<CommentDto>>.None();

        var boundary = tenantBoundaryValidator.EnsureTenantBoundary(
            logger, requestContext.TenantId, requestContext.Roles, entity.TenantId,
            "Comment:Get", nameof(Comment), entity.Id);
        if (boundary.IsFailure) return Result<DefaultResponse<CommentDto>>.Failure(boundary.ErrorMessage!);

        return HandlerHelpers.Success(entity.ToDto());
    }
}

/// <summary>Handles create comment work by coordinating validation, tenant boundaries, persistence, and response mapping.</summary>
internal sealed class CreateCommentHandler(
    ILogger<CreateCommentHandler> logger,
    IRequestContext<string, Guid?> requestContext,
    ICommentRepositoryTrxn repoTrxn,
    ITenantBoundaryValidator tenantBoundaryValidator)
    : IRequestHandler<CreateCommentCommand, Result<DefaultResponse<CommentDto>>>
{
    /// <summary>Handles create comment requests and returns the application result.</summary>
    public async Task<Result<DefaultResponse<CommentDto>>> HandleAsync(CreateCommentCommand command, CancellationToken ct = default)
    {
        var dto = command.Request.Item;
        dto.TenantId = requestContext.TenantId ?? Guid.Empty;

        var validation = CommentStructureValidator.ValidateCreate(dto);
        if (validation.IsFailure) return Result<DefaultResponse<CommentDto>>.Failure(validation.Errors);

        var boundary = tenantBoundaryValidator.EnsureTenantBoundary(
            logger, requestContext.TenantId, requestContext.Roles, dto.TenantId,
            "Comment:Create", nameof(Comment));
        if (boundary.IsFailure) return Result<DefaultResponse<CommentDto>>.Failure(boundary.ErrorMessage!);

        var entityResult = dto.ToEntity(dto.TenantId);
        if (entityResult.IsFailure) return Result<DefaultResponse<CommentDto>>.Failure(entityResult.ErrorMessage!);

        var entity = entityResult.Value!;
        repoTrxn.Create(ref entity);

        var save = await CqrsHandlerSupport.TrySaveAsync(repoTrxn, logger, "Error creating Comment", ct);
        if (save.IsFailure) return Result<DefaultResponse<CommentDto>>.Failure(save.ErrorMessage!);

        return HandlerHelpers.Success(entity.ToDto());
    }
}

/// <summary>Handles update comment work by coordinating validation, tenant boundaries, persistence, and response mapping.</summary>
internal sealed class UpdateCommentHandler(
    ILogger<UpdateCommentHandler> logger,
    IRequestContext<string, Guid?> requestContext,
    ICommentRepositoryTrxn repoTrxn,
    ITenantBoundaryValidator tenantBoundaryValidator)
    : IRequestHandler<UpdateCommentCommand, Result<DefaultResponse<CommentDto>>>
{
    /// <summary>Handles update comment requests and returns the application result.</summary>
    public async Task<Result<DefaultResponse<CommentDto>>> HandleAsync(UpdateCommentCommand command, CancellationToken ct = default)
    {
        var dto = command.Request.Item;
        dto.TenantId = requestContext.TenantId ?? Guid.Empty;

        var validation = CommentStructureValidator.ValidateUpdate(dto);
        if (validation.IsFailure) return Result<DefaultResponse<CommentDto>>.Failure(validation.Errors);

        var entity = await repoTrxn.GetCommentAsync(dto.Id!.Value, ct);
        if (entity is null)
        {
            return HandlerHelpers.NotFoundResponse<CommentDto>();
        }

        var boundary = tenantBoundaryValidator.EnsureTenantBoundary(
            logger, requestContext.TenantId, requestContext.Roles, entity.TenantId,
            "Comment:Update", nameof(Comment), entity.Id);
        if (boundary.IsFailure) return Result<DefaultResponse<CommentDto>>.Failure(boundary.ErrorMessage!);

        var tenantChangeCheck = tenantBoundaryValidator.PreventTenantChange(
            logger, entity.TenantId, dto.TenantId, nameof(Comment), entity.Id);
        if (tenantChangeCheck.IsFailure) return Result<DefaultResponse<CommentDto>>.Failure(tenantChangeCheck.ErrorMessage!);

        var updateResult = entity.Update(dto.Body);
        if (updateResult.IsFailure) return Result<DefaultResponse<CommentDto>>.Failure(updateResult.ErrorMessage!);

        var save = await CqrsHandlerSupport.TrySaveAsync(repoTrxn, logger, "Error updating Comment {Id}", ct, dto.Id);
        if (save.IsFailure) return Result<DefaultResponse<CommentDto>>.Failure(save.ErrorMessage!);

        return HandlerHelpers.Success(entity.ToDto());
    }
}

/// <summary>Handles delete comment work by coordinating validation, tenant boundaries, persistence, and response mapping.</summary>
internal sealed class DeleteCommentHandler(
    ILogger<DeleteCommentHandler> logger,
    IRequestContext<string, Guid?> requestContext,
    ICommentRepositoryTrxn repoTrxn,
    ITenantBoundaryValidator tenantBoundaryValidator,
    IEntityCacheProvider cache)
    : IRequestHandler<DeleteCommentCommand, Result>
{
    /// <summary>Handles delete comment requests and returns the application result.</summary>
    public async Task<Result> HandleAsync(DeleteCommentCommand command, CancellationToken ct = default)
    {
        var entity = await repoTrxn.GetCommentAsync(command.Id, ct);
        if (entity is null) return Result.Success();

        var boundary = tenantBoundaryValidator.EnsureTenantBoundary(
            logger, requestContext.TenantId, requestContext.Roles, entity.TenantId,
            "Comment:Delete", nameof(Comment), entity.Id);
        if (boundary.IsFailure) return Result.Failure(boundary.ErrorMessage!);

        repoTrxn.Delete(entity);

        var save = await CqrsHandlerSupport.TrySaveAsync(repoTrxn, logger, "Error deleting Comment {Id}", ct, command.Id);
        if (save.IsFailure) return save;

        await cache.RemoveAsync(HandlerHelpers.CacheKey(nameof(Comment), command.Id), ct);
        return Result.Success();
    }
}
