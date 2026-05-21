using EF.Common.Contracts;
using EF.CQRS.Abstractions;
using EF.Data.Contracts;
using Microsoft.Extensions.Logging;
using TaskFlow.Application.Contracts;
using TaskFlow.Application.Contracts.Repositories;
using TaskFlow.Application.Cqrs.Requests;
using TaskFlow.Application.Cqrs.Validation;
using TaskFlow.Application.Mappers;
using TaskFlow.Application.Models;
using TaskFlow.Domain.Model;

namespace TaskFlow.Application.Cqrs.Handlers;

internal sealed class SearchCommentsHandler(
    ILogger<SearchCommentsHandler> logger,
    IRequestContext<string, Guid?> requestContext,
    ICommentRepositoryQuery repoQuery)
    : IRequestHandler<SearchCommentsQuery, PagedResponse<CommentDto>>
{
    public async Task<PagedResponse<CommentDto>> HandleAsync(SearchCommentsQuery query, CancellationToken ct = default)
    {
        var request = query.Request;
        HandlerHelpers.EnforceTenantFilter(request, requestContext.TenantId, requestContext.Roles, logger, "CommentSearch");
        return await repoQuery.SearchCommentsAsync(request, ct);
    }
}

internal sealed class GetCommentByIdHandler(
    ILogger<GetCommentByIdHandler> logger,
    IRequestContext<string, Guid?> requestContext,
    ICommentRepositoryQuery repoQuery,
    ITenantBoundaryValidator tenantBoundaryValidator)
    : IRequestHandler<GetCommentByIdQuery, Result<DefaultResponse<CommentDto>>>
{
    public async Task<Result<DefaultResponse<CommentDto>>> HandleAsync(GetCommentByIdQuery query, CancellationToken ct = default)
    {
        var entity = await repoQuery.GetCommentAsync(query.Id, ct);
        if (entity is null) return Result<DefaultResponse<CommentDto>>.None();

        var boundary = tenantBoundaryValidator.EnsureTenantBoundary(
            logger, requestContext.TenantId, requestContext.Roles, entity.TenantId,
            "Comment:Get", nameof(Comment), entity.Id);
        if (boundary.IsFailure) return Result<DefaultResponse<CommentDto>>.Failure(boundary.ErrorMessage!);

        return Result<DefaultResponse<CommentDto>>.Success(HandlerHelpers.BuildResponse(entity.ToDto()));
    }
}

internal sealed class CreateCommentHandler(
    ILogger<CreateCommentHandler> logger,
    IRequestContext<string, Guid?> requestContext,
    ICommentRepositoryTrxn repoTrxn,
    ITenantBoundaryValidator tenantBoundaryValidator)
    : IRequestHandler<CreateCommentCommand, Result<DefaultResponse<CommentDto>>>
{
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

        try
        {
            await repoTrxn.SaveChangesAsync(OptimisticConcurrencyWinner.ClientWins, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating Comment");
            return Result<DefaultResponse<CommentDto>>.Failure(ex.GetBaseException().Message);
        }

        return Result<DefaultResponse<CommentDto>>.Success(HandlerHelpers.BuildResponse(entity.ToDto()));
    }
}

internal sealed class UpdateCommentHandler(
    ILogger<UpdateCommentHandler> logger,
    IRequestContext<string, Guid?> requestContext,
    ICommentRepositoryTrxn repoTrxn,
    ITenantBoundaryValidator tenantBoundaryValidator)
    : IRequestHandler<UpdateCommentCommand, Result<DefaultResponse<CommentDto>>>
{
    public async Task<Result<DefaultResponse<CommentDto>>> HandleAsync(UpdateCommentCommand command, CancellationToken ct = default)
    {
        var dto = command.Request.Item;
        dto.TenantId = requestContext.TenantId ?? Guid.Empty;

        var validation = CommentStructureValidator.ValidateUpdate(dto);
        if (validation.IsFailure) return Result<DefaultResponse<CommentDto>>.Failure(validation.Errors);

        var entity = await repoTrxn.GetCommentAsync(dto.Id!.Value, ct);
        if (entity is null)
        {
            return Result<DefaultResponse<CommentDto>>.Success(HandlerHelpers.BuildResponse<CommentDto>(null));
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

        try
        {
            await repoTrxn.SaveChangesAsync(OptimisticConcurrencyWinner.ClientWins, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating Comment {Id}", dto.Id);
            return Result<DefaultResponse<CommentDto>>.Failure(ex.GetBaseException().Message);
        }

        return Result<DefaultResponse<CommentDto>>.Success(HandlerHelpers.BuildResponse(entity.ToDto()));
    }
}

internal sealed class DeleteCommentHandler(
    ILogger<DeleteCommentHandler> logger,
    IRequestContext<string, Guid?> requestContext,
    ICommentRepositoryTrxn repoTrxn,
    ITenantBoundaryValidator tenantBoundaryValidator,
    IEntityCacheProvider cache)
    : IRequestHandler<DeleteCommentCommand, Result>
{
    public async Task<Result> HandleAsync(DeleteCommentCommand command, CancellationToken ct = default)
    {
        var entity = await repoTrxn.GetCommentAsync(command.Id, ct);
        if (entity is null) return Result.Success();

        var boundary = tenantBoundaryValidator.EnsureTenantBoundary(
            logger, requestContext.TenantId, requestContext.Roles, entity.TenantId,
            "Comment:Delete", nameof(Comment), entity.Id);
        if (boundary.IsFailure) return Result.Failure(boundary.ErrorMessage!);

        repoTrxn.Delete(entity);

        try
        {
            await repoTrxn.SaveChangesAsync(OptimisticConcurrencyWinner.ClientWins, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error deleting Comment {Id}", command.Id);
            return Result.Failure(ex.GetBaseException().Message);
        }

        await cache.RemoveAsync("Comment:" + command.Id, ct);
        return Result.Success();
    }
}
