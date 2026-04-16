using EF.Common.Contracts;
using TaskFlow.Application.Models;
using TaskFlow.Domain.Shared.Constants;

namespace TaskFlow.Application.Services.Rules;

internal static class AttachmentStructureValidator
{
    public static Result<AttachmentDto> ValidateCreate(AttachmentDto dto)
    {
        var common = StructureValidators.ValidateCreate(dto);
        if (common.IsFailure) return Result<AttachmentDto>.Failure(common.ErrorMessage!);

        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(dto.FileName)) errors.Add("FileName is required.");
        if (dto.FileName?.Length > DomainConstants.RULE_ATTACHMENT_FILENAME_LENGTH_MAX) errors.Add($"FileName cannot exceed {DomainConstants.RULE_ATTACHMENT_FILENAME_LENGTH_MAX} characters.");
        if (string.IsNullOrWhiteSpace(dto.ContentType)) errors.Add("ContentType is required.");
        if (dto.ContentType?.Length > DomainConstants.RULE_ATTACHMENT_CONTENTTYPE_LENGTH_MAX) errors.Add($"ContentType cannot exceed {DomainConstants.RULE_ATTACHMENT_CONTENTTYPE_LENGTH_MAX} characters.");
        if (dto.FileSizeBytes <= 0) errors.Add("FileSizeBytes must be greater than zero.");
        if (string.IsNullOrWhiteSpace(dto.StorageUri)) errors.Add("StorageUri is required.");
        if (dto.StorageUri?.Length > DomainConstants.RULE_ATTACHMENT_STORAGEURI_LENGTH_MAX) errors.Add($"StorageUri cannot exceed {DomainConstants.RULE_ATTACHMENT_STORAGEURI_LENGTH_MAX} characters.");
        if (dto.OwnerId == Guid.Empty) errors.Add("OwnerId is required.");
        return errors.Count > 0 ? Result<AttachmentDto>.Failure(errors) : Result<AttachmentDto>.Success(dto);
    }

    public static Result<AttachmentDto> ValidateUpdate(AttachmentDto dto)
    {
        var common = StructureValidators.ValidateUpdate(dto);
        if (common.IsFailure) return Result<AttachmentDto>.Failure(common.ErrorMessage!);

        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(dto.FileName)) errors.Add("FileName is required.");
        if (dto.FileName?.Length > DomainConstants.RULE_ATTACHMENT_FILENAME_LENGTH_MAX) errors.Add($"FileName cannot exceed {DomainConstants.RULE_ATTACHMENT_FILENAME_LENGTH_MAX} characters.");
        if (string.IsNullOrWhiteSpace(dto.ContentType)) errors.Add("ContentType is required.");
        if (dto.ContentType?.Length > DomainConstants.RULE_ATTACHMENT_CONTENTTYPE_LENGTH_MAX) errors.Add($"ContentType cannot exceed {DomainConstants.RULE_ATTACHMENT_CONTENTTYPE_LENGTH_MAX} characters.");
        if (dto.FileSizeBytes <= 0) errors.Add("FileSizeBytes must be greater than zero.");
        if (string.IsNullOrWhiteSpace(dto.StorageUri)) errors.Add("StorageUri is required.");
        if (dto.StorageUri?.Length > DomainConstants.RULE_ATTACHMENT_STORAGEURI_LENGTH_MAX) errors.Add($"StorageUri cannot exceed {DomainConstants.RULE_ATTACHMENT_STORAGEURI_LENGTH_MAX} characters.");
        return errors.Count > 0 ? Result<AttachmentDto>.Failure(errors) : Result<AttachmentDto>.Success(dto);
    }
}
