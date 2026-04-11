using EF.Domain.Contracts;
using TaskFlow.Application.Models;
using TaskFlow.Domain.Model;

namespace TaskFlow.Application.Mappers;

public static class AttachmentMapper
{
    public static AttachmentDto ToDto(this Attachment entity) => new()
    {
        Id = entity.Id,
        FileName = entity.FileName,
        ContentType = entity.ContentType,
        FileSizeBytes = entity.FileSizeBytes,
        StorageUri = entity.StorageUri,
        OwnerType = entity.OwnerType,
        OwnerId = entity.OwnerId
    };

    public static DomainResult<Attachment> ToEntity(this AttachmentDto dto, Guid tenantId)
        => Attachment.Create(tenantId, dto.FileName, dto.ContentType, dto.FileSizeBytes, dto.StorageUri, dto.OwnerType, dto.OwnerId);
}
