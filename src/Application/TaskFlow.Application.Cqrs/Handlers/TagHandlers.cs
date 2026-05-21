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

internal sealed class SearchTagsHandler(
    ILogger<SearchTagsHandler> logger,
    IRequestContext<string, Guid?> requestContext,
    ITagRepositoryQuery repoQuery)
    : IRequestHandler<SearchTagsQuery, PagedResponse<TagDto>>
{
    public async Task<PagedResponse<TagDto>> HandleAsync(SearchTagsQuery query, CancellationToken ct = default)
    {
        var request = query.Request;
        HandlerHelpers.EnforceTenantFilter(request, requestContext.TenantId, requestContext.Roles, logger, "TagSearch");
        return await repoQuery.SearchTagsAsync(request, ct);
    }
}

internal sealed class GetTagByIdHandler(
    ILogger<GetTagByIdHandler> logger,
    IRequestContext<string, Guid?> requestContext,
    ITagRepositoryQuery repoQuery,
    ITenantBoundaryValidator tenantBoundaryValidator)
    : IRequestHandler<GetTagByIdQuery, Result<DefaultResponse<TagDto>>>
{
    public async Task<Result<DefaultResponse<TagDto>>> HandleAsync(GetTagByIdQuery query, CancellationToken ct = default)
    {
        var entity = await repoQuery.GetTagAsync(query.Id, ct);
        if (entity is null) return Result<DefaultResponse<TagDto>>.None();

        var boundary = tenantBoundaryValidator.EnsureTenantBoundary(
            logger, requestContext.TenantId, requestContext.Roles, entity.TenantId,
            "Tag:Get", nameof(Tag), entity.Id);
        if (boundary.IsFailure) return Result<DefaultResponse<TagDto>>.Failure(boundary.ErrorMessage!);

        return Result<DefaultResponse<TagDto>>.Success(HandlerHelpers.BuildResponse(entity.ToDto()));
    }
}

internal sealed class CreateTagHandler(
    ILogger<CreateTagHandler> logger,
    IRequestContext<string, Guid?> requestContext,
    ITagRepositoryTrxn repoTrxn,
    ITenantBoundaryValidator tenantBoundaryValidator)
    : IRequestHandler<CreateTagCommand, Result<DefaultResponse<TagDto>>>
{
    public async Task<Result<DefaultResponse<TagDto>>> HandleAsync(CreateTagCommand command, CancellationToken ct = default)
    {
        var dto = command.Request.Item;
        dto.TenantId = requestContext.TenantId ?? Guid.Empty;

        var validation = TagStructureValidator.ValidateCreate(dto);
        if (validation.IsFailure) return Result<DefaultResponse<TagDto>>.Failure(validation.Errors);

        var boundary = tenantBoundaryValidator.EnsureTenantBoundary(
            logger, requestContext.TenantId, requestContext.Roles, dto.TenantId,
            "Tag:Create", nameof(Tag));
        if (boundary.IsFailure) return Result<DefaultResponse<TagDto>>.Failure(boundary.ErrorMessage!);

        var entityResult = dto.ToEntity(dto.TenantId);
        if (entityResult.IsFailure) return Result<DefaultResponse<TagDto>>.Failure(entityResult.ErrorMessage!);

        var entity = entityResult.Value!;
        repoTrxn.Create(ref entity);

        try
        {
            await repoTrxn.SaveChangesAsync(OptimisticConcurrencyWinner.ClientWins, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating Tag");
            return Result<DefaultResponse<TagDto>>.Failure(ex.GetBaseException().Message);
        }

        return Result<DefaultResponse<TagDto>>.Success(HandlerHelpers.BuildResponse(entity.ToDto()));
    }
}

internal sealed class UpdateTagHandler(
    ILogger<UpdateTagHandler> logger,
    IRequestContext<string, Guid?> requestContext,
    ITagRepositoryTrxn repoTrxn,
    ITenantBoundaryValidator tenantBoundaryValidator)
    : IRequestHandler<UpdateTagCommand, Result<DefaultResponse<TagDto>>>
{
    public async Task<Result<DefaultResponse<TagDto>>> HandleAsync(UpdateTagCommand command, CancellationToken ct = default)
    {
        var dto = command.Request.Item;
        dto.TenantId = requestContext.TenantId ?? Guid.Empty;

        var validation = TagStructureValidator.ValidateUpdate(dto);
        if (validation.IsFailure) return Result<DefaultResponse<TagDto>>.Failure(validation.Errors);

        var entity = await repoTrxn.GetTagAsync(dto.Id!.Value, ct);
        if (entity is null)
        {
            return Result<DefaultResponse<TagDto>>.Success(HandlerHelpers.BuildResponse<TagDto>(null));
        }

        var boundary = tenantBoundaryValidator.EnsureTenantBoundary(
            logger, requestContext.TenantId, requestContext.Roles, entity.TenantId,
            "Tag:Update", nameof(Tag), entity.Id);
        if (boundary.IsFailure) return Result<DefaultResponse<TagDto>>.Failure(boundary.ErrorMessage!);

        var tenantChangeCheck = tenantBoundaryValidator.PreventTenantChange(
            logger, entity.TenantId, dto.TenantId, nameof(Tag), entity.Id);
        if (tenantChangeCheck.IsFailure) return Result<DefaultResponse<TagDto>>.Failure(tenantChangeCheck.ErrorMessage!);

        var updateResult = entity.Update(dto.Name, dto.Color);
        if (updateResult.IsFailure) return Result<DefaultResponse<TagDto>>.Failure(updateResult.ErrorMessage!);

        try
        {
            await repoTrxn.SaveChangesAsync(OptimisticConcurrencyWinner.ClientWins, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating Tag {Id}", dto.Id);
            return Result<DefaultResponse<TagDto>>.Failure(ex.GetBaseException().Message);
        }

        return Result<DefaultResponse<TagDto>>.Success(HandlerHelpers.BuildResponse(entity.ToDto()));
    }
}

internal sealed class DeleteTagHandler(
    ILogger<DeleteTagHandler> logger,
    IRequestContext<string, Guid?> requestContext,
    ITagRepositoryTrxn repoTrxn,
    ITenantBoundaryValidator tenantBoundaryValidator,
    IEntityCacheProvider cache)
    : IRequestHandler<DeleteTagCommand, Result>
{
    public async Task<Result> HandleAsync(DeleteTagCommand command, CancellationToken ct = default)
    {
        var entity = await repoTrxn.GetTagAsync(command.Id, ct);
        if (entity is null) return Result.Success();

        var boundary = tenantBoundaryValidator.EnsureTenantBoundary(
            logger, requestContext.TenantId, requestContext.Roles, entity.TenantId,
            "Tag:Delete", nameof(Tag), entity.Id);
        if (boundary.IsFailure) return Result.Failure(boundary.ErrorMessage!);

        repoTrxn.Delete(entity);

        try
        {
            await repoTrxn.SaveChangesAsync(OptimisticConcurrencyWinner.ClientWins, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error deleting Tag {Id}", command.Id);
            return Result.Failure(ex.GetBaseException().Message);
        }

        await cache.RemoveAsync("Tag:" + command.Id, ct);
        return Result.Success();
    }
}
