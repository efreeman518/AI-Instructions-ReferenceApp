using EF.Common.Contracts;
using EF.Data.Contracts;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TaskFlow.Application.Contracts;
using TaskFlow.Application.Contracts.Repositories;
using TaskFlow.Application.Contracts.Services;
using TaskFlow.Application.Contracts.Storage;
using TaskFlow.Application.Mappers;
using TaskFlow.Application.Models;
using TaskFlow.Application.Services.Rules;

namespace TaskFlow.Application.Services;

internal class AttachmentService(
    ILogger<AttachmentService> logger,
    IRequestContext<string, Guid?> requestContext,
    IAttachmentRepositoryTrxn repoTrxn,
    IAttachmentRepositoryQuery repoQuery,
    ITenantBoundaryValidator tenantBoundaryValidator,
    IEntityCacheProvider cache,
    IBlobStorageRepository? blobStorage = null) : IAttachmentService
{
    private Guid? RequestTenantId => requestContext.TenantId;
    private IReadOnlyCollection<string> RequestRoles => requestContext.Roles;
    private bool IsGlobalAdmin => RequestRoles.Contains(AppConstants.ROLE_GLOBAL_ADMIN);

    public async Task<PagedResponse<AttachmentDto>> SearchAsync(
        SearchRequest<AttachmentSearchFilter> request, CancellationToken ct = default)
    {
        if (!IsGlobalAdmin)
        {
            request.Filter ??= new();
            request.Filter.TenantId = RequestTenantId;
        }
        var page = await repoQuery.SearchAttachmentsAsync(request, ct);
        return new PagedResponse<AttachmentDto>
        {
            Data = page.Data.Select(e => e.ToDto()).ToList(),
            Total = page.Total,
            PageSize = page.PageSize,
            PageIndex = page.PageIndex
        };
    }

    public async Task<Result<DefaultResponse<AttachmentDto>>> GetAsync(Guid id, CancellationToken ct = default)
    {
        var entity = await repoQuery.GetAttachmentAsync(id, ct);
        if (entity == null) return Result<DefaultResponse<AttachmentDto>>.None();

        var boundary = tenantBoundaryValidator.EnsureTenantBoundary(
            logger, RequestTenantId, RequestRoles, entity.TenantId,
            "Attachment:Get", "Attachment", entity.Id);
        if (boundary.IsFailure) return Result<DefaultResponse<AttachmentDto>>.Failure(boundary.ErrorMessage!);

        return Result<DefaultResponse<AttachmentDto>>.Success(new() { Item = entity.ToDto() });
    }

    public async Task<Result<DefaultResponse<AttachmentDto>>> CreateAsync(
        DefaultRequest<AttachmentDto> request, CancellationToken ct = default)
    {
        var dto = request.Item;

        var validation = AttachmentStructureValidator.ValidateCreate(dto);
        if (validation.IsFailure) return Result<DefaultResponse<AttachmentDto>>.Failure(validation.Errors);

        var boundary = tenantBoundaryValidator.EnsureTenantBoundary(
            logger, RequestTenantId, RequestRoles, RequestTenantId,
            "Attachment:Create", "Attachment");
        if (boundary.IsFailure) return Result<DefaultResponse<AttachmentDto>>.Failure(boundary.ErrorMessage!);

        var entityResult = dto.ToEntity(RequestTenantId ?? Guid.Empty);
        if (entityResult.IsFailure) return Result<DefaultResponse<AttachmentDto>>.Failure(entityResult.ErrorMessage!);

        var entity = entityResult.Value!;
        repoTrxn.Create(ref entity);

        try
        {
            await repoTrxn.SaveChangesAsync(OptimisticConcurrencyWinner.ClientWins, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating Attachment");
            return Result<DefaultResponse<AttachmentDto>>.Failure(ex.GetBaseException().Message);
        }

        var resultDto = entity.ToDto();
        await cache.SetAsync($"Attachment:{entity.Id}", resultDto, ct);
        return Result<DefaultResponse<AttachmentDto>>.Success(new() { Item = resultDto });
    }

    public async Task<Result<DefaultResponse<AttachmentDto>>> UpdateAsync(
        DefaultRequest<AttachmentDto> request, CancellationToken ct = default)
    {
        var dto = request.Item;

        var validation = AttachmentStructureValidator.ValidateUpdate(dto);
        if (validation.IsFailure) return Result<DefaultResponse<AttachmentDto>>.Failure(validation.Errors);

        var entity = await repoTrxn.GetAttachmentAsync(dto.Id!.Value, ct);
        if (entity == null) return Result<DefaultResponse<AttachmentDto>>.Success(new() { Item = null });

        var boundary = tenantBoundaryValidator.EnsureTenantBoundary(
            logger, RequestTenantId, RequestRoles, entity.TenantId,
            "Attachment:Update", "Attachment", entity.Id);
        if (boundary.IsFailure) return Result<DefaultResponse<AttachmentDto>>.Failure(boundary.ErrorMessage!);

        var updateResult = entity.Update(dto.FileName, dto.ContentType, dto.FileSizeBytes, dto.StorageUri);
        if (updateResult.IsFailure) return Result<DefaultResponse<AttachmentDto>>.Failure(updateResult.ErrorMessage!);

        try
        {
            await repoTrxn.SaveChangesAsync(OptimisticConcurrencyWinner.ClientWins, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating Attachment {Id}", dto.Id);
            return Result<DefaultResponse<AttachmentDto>>.Failure(ex.GetBaseException().Message);
        }

        var resultDto = entity.ToDto();
        await cache.SetAsync($"Attachment:{entity.Id}", resultDto, ct);
        return Result<DefaultResponse<AttachmentDto>>.Success(new() { Item = resultDto });
    }

    public async Task<Result> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var entity = await repoTrxn.GetAttachmentAsync(id, ct);
        if (entity == null) return Result.Success();

        var boundary = tenantBoundaryValidator.EnsureTenantBoundary(
            logger, RequestTenantId, RequestRoles, entity.TenantId,
            "Attachment:Delete", "Attachment", entity.Id);
        if (boundary.IsFailure) return Result.Failure(boundary.ErrorMessage!);

        repoTrxn.Delete(entity);

        try
        {
            await repoTrxn.SaveChangesAsync(OptimisticConcurrencyWinner.ClientWins, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error deleting Attachment {Id}", id);
            return Result.Failure(ex.GetBaseException().Message);
        }

        // Delete blob from storage if available
        if (blobStorage is not null && !string.IsNullOrEmpty(entity.StorageUri))
        {
            try
            {
                var blobName = $"{entity.TenantId}/{entity.OwnerId}/{entity.FileName}";
                await blobStorage.DeleteAsync("attachments", blobName, ct);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to delete blob for Attachment {Id}", id);
            }
        }

        await cache.RemoveAsync($"Attachment:{id}", ct);
        return Result.Success();
    }
}
