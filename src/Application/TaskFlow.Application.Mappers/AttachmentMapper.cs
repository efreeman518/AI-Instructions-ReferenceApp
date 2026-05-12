using System.Linq.Expressions;
using EF.Domain.Contracts;
using TaskFlow.Application.Models;
using TaskFlow.Domain.Model;

namespace TaskFlow.Application.Mappers;

public static class AttachmentMapper
{
    public static readonly Expression<Func<Attachment, AttachmentDto>> Projection =
        entity => new AttachmentDto
        {
            Id = entity.Id,
            TenantId = entity.TenantId,
            FileName = entity.FileName,
            ContentType = entity.ContentType,
            FileSizeBytes = entity.FileSizeBytes,
            StorageUri = entity.StorageUri,
            OwnerType = entity.OwnerType,
            OwnerId = entity.OwnerId
        };

    private static readonly Func<Attachment, AttachmentDto> Compiled = Projection.Compile();

    public static AttachmentDto ToDto(this Attachment entity) => Compiled(entity);

    public static DomainResult<Attachment> ToEntity(this AttachmentDto dto, Guid tenantId)
        => Attachment.Create(tenantId, dto.FileName, dto.ContentType, dto.FileSizeBytes, dto.StorageUri, dto.OwnerType, dto.OwnerId);
}
