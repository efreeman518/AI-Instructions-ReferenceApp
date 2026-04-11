using EF.Common.Contracts;
using EF.Data.Contracts;
using Microsoft.Extensions.Logging;
using TaskFlow.Application.Contracts;
using TaskFlow.Application.Contracts.Repositories;
using TaskFlow.Application.Contracts.Services;
using TaskFlow.Application.Mappers;
using TaskFlow.Application.Models;
using TaskFlow.Application.Services.Rules;

namespace TaskFlow.Application.Services;

internal class CommentService(
    ILogger<CommentService> logger,
    IRequestContext<string, Guid?> requestContext,
    ICommentRepositoryTrxn repoTrxn,
    ICommentRepositoryQuery repoQuery,
    ITenantBoundaryValidator tenantBoundaryValidator,
    IEntityCacheProvider cache) : ICommentService
{
    private Guid? RequestTenantId => requestContext.TenantId;
    private IReadOnlyCollection<string> RequestRoles => requestContext.Roles;
    private bool IsGlobalAdmin => RequestRoles.Contains(AppConstants.ROLE_GLOBAL_ADMIN);

    public async Task<PagedResponse<CommentDto>> SearchAsync(
        SearchRequest<CommentSearchFilter> request, CancellationToken ct = default)
    {
        if (!IsGlobalAdmin)
        {
            request.Filter ??= new();
            request.Filter.TenantId = RequestTenantId;
        }
        var page = await repoQuery.SearchCommentsAsync(request, ct);
        return new PagedResponse<CommentDto>
        {
            Data = page.Data.Select(e => e.ToDto()).ToList(),
            Total = page.Total,
            PageSize = page.PageSize,
            PageIndex = page.PageIndex
        };
    }

    public async Task<Result<DefaultResponse<CommentDto>>> GetAsync(Guid id, CancellationToken ct = default)
    {
        var entity = await repoQuery.GetCommentAsync(id, ct);
        if (entity == null) return Result<DefaultResponse<CommentDto>>.None();

        var boundary = tenantBoundaryValidator.EnsureTenantBoundary(
            logger, RequestTenantId, RequestRoles, entity.TenantId,
            "Comment:Get", "Comment", entity.Id);
        if (boundary.IsFailure) return Result<DefaultResponse<CommentDto>>.Failure(boundary.ErrorMessage!);

        return Result<DefaultResponse<CommentDto>>.Success(new() { Item = entity.ToDto() });
    }

    public async Task<Result<DefaultResponse<CommentDto>>> CreateAsync(
        DefaultRequest<CommentDto> request, CancellationToken ct = default)
    {
        var dto = request.Item;

        var validation = CommentStructureValidator.ValidateCreate(dto);
        if (validation.IsFailure) return Result<DefaultResponse<CommentDto>>.Failure(validation.Errors);

        var boundary = tenantBoundaryValidator.EnsureTenantBoundary(
            logger, RequestTenantId, RequestRoles, RequestTenantId,
            "Comment:Create", "Comment");
        if (boundary.IsFailure) return Result<DefaultResponse<CommentDto>>.Failure(boundary.ErrorMessage!);

        var entityResult = dto.ToEntity(RequestTenantId ?? Guid.Empty);
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

        var resultDto = entity.ToDto();
        await cache.SetAsync($"Comment:{entity.Id}", resultDto, ct);
        return Result<DefaultResponse<CommentDto>>.Success(new() { Item = resultDto });
    }

    public async Task<Result<DefaultResponse<CommentDto>>> UpdateAsync(
        DefaultRequest<CommentDto> request, CancellationToken ct = default)
    {
        var dto = request.Item;

        var validation = CommentStructureValidator.ValidateUpdate(dto);
        if (validation.IsFailure) return Result<DefaultResponse<CommentDto>>.Failure(validation.Errors);

        var entity = await repoTrxn.GetCommentAsync(dto.Id!.Value, ct);
        if (entity == null) return Result<DefaultResponse<CommentDto>>.Success(new() { Item = null });

        var boundary = tenantBoundaryValidator.EnsureTenantBoundary(
            logger, RequestTenantId, RequestRoles, entity.TenantId,
            "Comment:Update", "Comment", entity.Id);
        if (boundary.IsFailure) return Result<DefaultResponse<CommentDto>>.Failure(boundary.ErrorMessage!);

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

        var resultDto = entity.ToDto();
        await cache.SetAsync($"Comment:{entity.Id}", resultDto, ct);
        return Result<DefaultResponse<CommentDto>>.Success(new() { Item = resultDto });
    }

    public async Task<Result> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var entity = await repoTrxn.GetCommentAsync(id, ct);
        if (entity == null) return Result.Success();

        var boundary = tenantBoundaryValidator.EnsureTenantBoundary(
            logger, RequestTenantId, RequestRoles, entity.TenantId,
            "Comment:Delete", "Comment", entity.Id);
        if (boundary.IsFailure) return Result.Failure(boundary.ErrorMessage!);

        repoTrxn.Delete(entity);

        try
        {
            await repoTrxn.SaveChangesAsync(OptimisticConcurrencyWinner.ClientWins, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error deleting Comment {Id}", id);
            return Result.Failure(ex.GetBaseException().Message);
        }

        await cache.RemoveAsync($"Comment:{id}", ct);
        return Result.Success();
    }
}
