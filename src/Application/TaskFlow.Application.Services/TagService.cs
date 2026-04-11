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

internal class TagService(
    ILogger<TagService> logger,
    IRequestContext<string, Guid?> requestContext,
    ITagRepositoryTrxn repoTrxn,
    ITagRepositoryQuery repoQuery,
    ITenantBoundaryValidator tenantBoundaryValidator,
    IEntityCacheProvider cache) : ITagService
{
    private Guid? RequestTenantId => requestContext.TenantId;
    private IReadOnlyCollection<string> RequestRoles => requestContext.Roles;
    private bool IsGlobalAdmin => RequestRoles.Contains(AppConstants.ROLE_GLOBAL_ADMIN);

    public async Task<PagedResponse<TagDto>> SearchAsync(
        SearchRequest<TagSearchFilter> request, CancellationToken ct = default)
    {
        if (!IsGlobalAdmin)
        {
            request.Filter ??= new();
            request.Filter.TenantId = RequestTenantId;
        }
        var page = await repoQuery.SearchTagsAsync(request, ct);
        return new PagedResponse<TagDto>
        {
            Data = page.Data.Select(e => e.ToDto()).ToList(),
            Total = page.Total,
            PageSize = page.PageSize,
            PageIndex = page.PageIndex
        };
    }

    public async Task<Result<DefaultResponse<TagDto>>> GetAsync(Guid id, CancellationToken ct = default)
    {
        var entity = await repoQuery.GetTagAsync(id, ct);
        if (entity == null) return Result<DefaultResponse<TagDto>>.None();

        var boundary = tenantBoundaryValidator.EnsureTenantBoundary(
            logger, RequestTenantId, RequestRoles, entity.TenantId,
            "Tag:Get", "Tag", entity.Id);
        if (boundary.IsFailure) return Result<DefaultResponse<TagDto>>.Failure(boundary.ErrorMessage!);

        return Result<DefaultResponse<TagDto>>.Success(new() { Item = entity.ToDto() });
    }

    public async Task<Result<DefaultResponse<TagDto>>> CreateAsync(
        DefaultRequest<TagDto> request, CancellationToken ct = default)
    {
        var dto = request.Item;

        var validation = TagStructureValidator.ValidateCreate(dto);
        if (validation.IsFailure) return Result<DefaultResponse<TagDto>>.Failure(validation.Errors);

        var boundary = tenantBoundaryValidator.EnsureTenantBoundary(
            logger, RequestTenantId, RequestRoles, RequestTenantId,
            "Tag:Create", "Tag");
        if (boundary.IsFailure) return Result<DefaultResponse<TagDto>>.Failure(boundary.ErrorMessage!);

        var entityResult = dto.ToEntity(RequestTenantId ?? Guid.Empty);
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

        var resultDto = entity.ToDto();
        await cache.SetAsync($"Tag:{entity.Id}", resultDto, ct);
        return Result<DefaultResponse<TagDto>>.Success(new() { Item = resultDto });
    }

    public async Task<Result<DefaultResponse<TagDto>>> UpdateAsync(
        DefaultRequest<TagDto> request, CancellationToken ct = default)
    {
        var dto = request.Item;

        var validation = TagStructureValidator.ValidateUpdate(dto);
        if (validation.IsFailure) return Result<DefaultResponse<TagDto>>.Failure(validation.Errors);

        var entity = await repoTrxn.GetTagAsync(dto.Id!.Value, ct);
        if (entity == null) return Result<DefaultResponse<TagDto>>.Success(new() { Item = null });

        var boundary = tenantBoundaryValidator.EnsureTenantBoundary(
            logger, RequestTenantId, RequestRoles, entity.TenantId,
            "Tag:Update", "Tag", entity.Id);
        if (boundary.IsFailure) return Result<DefaultResponse<TagDto>>.Failure(boundary.ErrorMessage!);

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

        var resultDto = entity.ToDto();
        await cache.SetAsync($"Tag:{entity.Id}", resultDto, ct);
        return Result<DefaultResponse<TagDto>>.Success(new() { Item = resultDto });
    }

    public async Task<Result> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var entity = await repoTrxn.GetTagAsync(id, ct);
        if (entity == null) return Result.Success();

        var boundary = tenantBoundaryValidator.EnsureTenantBoundary(
            logger, RequestTenantId, RequestRoles, entity.TenantId,
            "Tag:Delete", "Tag", entity.Id);
        if (boundary.IsFailure) return Result.Failure(boundary.ErrorMessage!);

        repoTrxn.Delete(entity);

        try
        {
            await repoTrxn.SaveChangesAsync(OptimisticConcurrencyWinner.ClientWins, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error deleting Tag {Id}", id);
            return Result.Failure(ex.GetBaseException().Message);
        }

        await cache.RemoveAsync($"Tag:{id}", ct);
        return Result.Success();
    }
}
