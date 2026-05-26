using EF.Common.Contracts;
using TaskFlow.Application.Cqrs.Shared;
using EF.Data.Contracts;
using Microsoft.Extensions.Logging;
using TaskFlow.Application.Contracts;
using TaskFlow.Application.Contracts.Repositories;
using TaskFlow.Application.Contracts.Storage;
using EF.CQRS.Abstractions;
using TaskFlow.Application.Mappers;
using TaskFlow.Application.Models;
using TaskFlow.Domain.Model;

namespace TaskFlow.Application.Cqrs.Features.Attachments;

internal sealed class SearchAttachmentsHandler(
    ILogger<SearchAttachmentsHandler> logger,
    IRequestContext<string, Guid?> requestContext,
    IAttachmentRepositoryQuery repoQuery)
    : IRequestHandler<SearchAttachmentsQuery, PagedResponse<AttachmentDto>>
{
    public async Task<PagedResponse<AttachmentDto>> HandleAsync(SearchAttachmentsQuery query, CancellationToken ct = default)
    {
        var request = query.Request;
        HandlerHelpers.EnforceTenantFilter(request, requestContext.TenantId, requestContext.Roles, logger, "AttachmentSearch");
        return await CqrsHandlerSupport.SearchAsync(token => repoQuery.SearchAttachmentsAsync(request, token), logger, "Attachment", ct);
    }
}

internal sealed class GetAttachmentByIdHandler(
    ILogger<GetAttachmentByIdHandler> logger,
    IRequestContext<string, Guid?> requestContext,
    IAttachmentRepositoryQuery repoQuery,
    ITenantBoundaryValidator tenantBoundaryValidator)
    : IRequestHandler<GetAttachmentByIdQuery, Result<DefaultResponse<AttachmentDto>>>
{
    public async Task<Result<DefaultResponse<AttachmentDto>>> HandleAsync(GetAttachmentByIdQuery query, CancellationToken ct = default)
    {
        var entity = await repoQuery.GetAttachmentAsync(query.Id, ct);
        if (entity is null) return Result<DefaultResponse<AttachmentDto>>.None();

        var boundary = tenantBoundaryValidator.EnsureTenantBoundary(
            logger, requestContext.TenantId, requestContext.Roles, entity.TenantId,
            "Attachment:Get", nameof(Attachment), entity.Id);
        if (boundary.IsFailure) return Result<DefaultResponse<AttachmentDto>>.Failure(boundary.ErrorMessage!);

        return HandlerHelpers.Success(entity.ToDto());
    }
}

internal sealed class CreateAttachmentHandler(
    ILogger<CreateAttachmentHandler> logger,
    IRequestContext<string, Guid?> requestContext,
    IAttachmentRepositoryTrxn repoTrxn,
    ITenantBoundaryValidator tenantBoundaryValidator)
    : IRequestHandler<CreateAttachmentCommand, Result<DefaultResponse<AttachmentDto>>>
{
    public async Task<Result<DefaultResponse<AttachmentDto>>> HandleAsync(CreateAttachmentCommand command, CancellationToken ct = default)
    {
        var dto = command.Request.Item;
        dto.TenantId = requestContext.TenantId ?? Guid.Empty;

        var validation = AttachmentStructureValidator.ValidateCreate(dto);
        if (validation.IsFailure) return Result<DefaultResponse<AttachmentDto>>.Failure(validation.Errors);

        var boundary = tenantBoundaryValidator.EnsureTenantBoundary(
            logger, requestContext.TenantId, requestContext.Roles, dto.TenantId,
            "Attachment:Create", nameof(Attachment));
        if (boundary.IsFailure) return Result<DefaultResponse<AttachmentDto>>.Failure(boundary.ErrorMessage!);

        var entityResult = dto.ToEntity(dto.TenantId);
        if (entityResult.IsFailure) return Result<DefaultResponse<AttachmentDto>>.Failure(entityResult.ErrorMessage!);

        var entity = entityResult.Value!;
        repoTrxn.Create(ref entity);

        var save = await CqrsHandlerSupport.TrySaveAsync(repoTrxn, logger, "Error creating Attachment", ct);
        if (save.IsFailure) return Result<DefaultResponse<AttachmentDto>>.Failure(save.ErrorMessage!);

        return HandlerHelpers.Success(entity.ToDto());
    }
}

internal sealed class UploadAttachmentHandler(
    ILogger<UploadAttachmentHandler> logger,
    IRequestContext<string, Guid?> requestContext,
    IAttachmentRepositoryTrxn repoTrxn,
    ITenantBoundaryValidator tenantBoundaryValidator,
    IBlobStorageRepository? blobStorage = null)
    : IRequestHandler<UploadAttachmentCommand, Result<DefaultResponse<AttachmentDto>>>
{
    public async Task<Result<DefaultResponse<AttachmentDto>>> HandleAsync(UploadAttachmentCommand command, CancellationToken ct = default)
    {
        var boundary = tenantBoundaryValidator.EnsureTenantBoundary(
            logger, requestContext.TenantId, requestContext.Roles, requestContext.TenantId,
            "Attachment:Upload", nameof(Attachment));
        if (boundary.IsFailure) return Result<DefaultResponse<AttachmentDto>>.Failure(boundary.ErrorMessage!);

        if (blobStorage is null)
            return Result<DefaultResponse<AttachmentDto>>.Failure("Blob storage is not configured.");

        var tenantId = requestContext.TenantId ?? Guid.Empty;
        var blobName = $"{tenantId}/{command.OwnerId}/{command.FileName}";

        try
        {
            await blobStorage.UploadAsync("attachments", blobName, command.FileStream, command.ContentType, ct: ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error uploading blob for Attachment {FileName}", command.FileName);
            return Result<DefaultResponse<AttachmentDto>>.Failure($"Blob upload failed: {ex.GetBaseException().Message}");
        }

        var storageUri = (await blobStorage.GetBlobUriAsync("attachments", blobName, ct)).ToString();
        var entityResult = Attachment.Create(
            tenantId,
            command.FileName,
            command.ContentType,
            command.FileSizeBytes,
            storageUri,
            command.OwnerType,
            command.OwnerId);
        if (entityResult.IsFailure) return Result<DefaultResponse<AttachmentDto>>.Failure(entityResult.ErrorMessage!);

        var entity = entityResult.Value!;
        repoTrxn.Create(ref entity);

        var save = await CqrsHandlerSupport.TrySaveAsync(repoTrxn, logger, "Error persisting Attachment after upload", ct);
        if (save.IsFailure) return Result<DefaultResponse<AttachmentDto>>.Failure(save.ErrorMessage!);

        return HandlerHelpers.Success(entity.ToDto());
    }
}

internal sealed class UpdateAttachmentHandler(
    ILogger<UpdateAttachmentHandler> logger,
    IRequestContext<string, Guid?> requestContext,
    IAttachmentRepositoryTrxn repoTrxn,
    ITenantBoundaryValidator tenantBoundaryValidator)
    : IRequestHandler<UpdateAttachmentCommand, Result<DefaultResponse<AttachmentDto>>>
{
    public async Task<Result<DefaultResponse<AttachmentDto>>> HandleAsync(UpdateAttachmentCommand command, CancellationToken ct = default)
    {
        var dto = command.Request.Item;
        dto.TenantId = requestContext.TenantId ?? Guid.Empty;

        var validation = AttachmentStructureValidator.ValidateUpdate(dto);
        if (validation.IsFailure) return Result<DefaultResponse<AttachmentDto>>.Failure(validation.Errors);

        var entity = await repoTrxn.GetAttachmentAsync(dto.Id!.Value, ct);
        if (entity is null)
        {
            return HandlerHelpers.NotFoundResponse<AttachmentDto>();
        }

        var boundary = tenantBoundaryValidator.EnsureTenantBoundary(
            logger, requestContext.TenantId, requestContext.Roles, entity.TenantId,
            "Attachment:Update", nameof(Attachment), entity.Id);
        if (boundary.IsFailure) return Result<DefaultResponse<AttachmentDto>>.Failure(boundary.ErrorMessage!);

        var tenantChangeCheck = tenantBoundaryValidator.PreventTenantChange(
            logger, entity.TenantId, dto.TenantId, nameof(Attachment), entity.Id);
        if (tenantChangeCheck.IsFailure) return Result<DefaultResponse<AttachmentDto>>.Failure(tenantChangeCheck.ErrorMessage!);

        var updateResult = entity.Update(dto.FileName, dto.ContentType, dto.FileSizeBytes, dto.StorageUri);
        if (updateResult.IsFailure) return Result<DefaultResponse<AttachmentDto>>.Failure(updateResult.ErrorMessage!);

        var save = await CqrsHandlerSupport.TrySaveAsync(repoTrxn, logger, "Error updating Attachment {Id}", ct, dto.Id);
        if (save.IsFailure) return Result<DefaultResponse<AttachmentDto>>.Failure(save.ErrorMessage!);

        return HandlerHelpers.Success(entity.ToDto());
    }
}

internal sealed class DeleteAttachmentHandler(
    ILogger<DeleteAttachmentHandler> logger,
    IRequestContext<string, Guid?> requestContext,
    IAttachmentRepositoryTrxn repoTrxn,
    ITenantBoundaryValidator tenantBoundaryValidator,
    IEntityCacheProvider cache,
    IBlobStorageRepository? blobStorage = null)
    : IRequestHandler<DeleteAttachmentCommand, Result>
{
    public async Task<Result> HandleAsync(DeleteAttachmentCommand command, CancellationToken ct = default)
    {
        var entity = await repoTrxn.GetAttachmentAsync(command.Id, ct);
        if (entity is null) return Result.Success();

        var boundary = tenantBoundaryValidator.EnsureTenantBoundary(
            logger, requestContext.TenantId, requestContext.Roles, entity.TenantId,
            "Attachment:Delete", nameof(Attachment), entity.Id);
        if (boundary.IsFailure) return Result.Failure(boundary.ErrorMessage!);

        repoTrxn.Delete(entity);

        var save = await CqrsHandlerSupport.TrySaveAsync(repoTrxn, logger, "Error deleting Attachment {Id}", ct, command.Id);
        if (save.IsFailure) return save;

        if (blobStorage is not null && !string.IsNullOrEmpty(entity.StorageUri))
        {
            try
            {
                var blobName = $"{entity.TenantId}/{entity.OwnerId}/{entity.FileName}";
                await blobStorage.DeleteAsync("attachments", blobName, ct);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to delete blob for Attachment {Id}", command.Id);
            }
        }

        await cache.RemoveAsync(HandlerHelpers.CacheKey(nameof(Attachment), command.Id), ct);
        return Result.Success();
    }
}
