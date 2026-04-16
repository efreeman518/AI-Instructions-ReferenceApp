using EF.Common.Contracts;
using EF.Data.Contracts;
using TaskFlow.Application.Models;
using TaskFlow.Domain.Model;
using TaskFlow.Domain.Shared.Enums;

namespace TaskFlow.Application.Contracts.Repositories;

public interface IAttachmentRepositoryQuery : IRepositoryBase
{
    Task<Attachment?> GetAttachmentAsync(Guid id, CancellationToken ct = default);
    Task<PagedResponse<AttachmentDto>> SearchAttachmentsAsync(SearchRequest<AttachmentSearchFilter> request, CancellationToken ct = default);
    Task<int> CountByOwnerAsync(AttachmentOwnerType ownerType, Guid ownerId, CancellationToken ct = default);
}
