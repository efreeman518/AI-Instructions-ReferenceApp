using EF.Common.Contracts;
using EF.Data.Contracts;
using TaskFlow.Application.Models;
using TaskFlow.Domain.Model;
using TaskFlow.Domain.Shared.Enums;

namespace TaskFlow.Application.Contracts.Repositories;

/// <summary>Persists and queries i attachment data through infrastructure storage contracts.</summary>
public interface IAttachmentRepositoryQuery : IRepositoryBase
{
    /// <summary>Loads requested data and maps missing records to the expected response.</summary>
    Task<Attachment?> GetAttachmentAsync(Guid id, CancellationToken ct = default);
    /// <summary>Searches search attachments and returns filtered results for callers.</summary>
    Task<PagedResponse<AttachmentDto>> SearchAttachmentsAsync(SearchRequest<AttachmentSearchFilter> request, CancellationToken ct = default);
    /// <summary>Provides the count by owner operation for attachment repository query.</summary>
    Task<int> CountByOwnerAsync(AttachmentOwnerType ownerType, Guid ownerId, CancellationToken ct = default);
}
