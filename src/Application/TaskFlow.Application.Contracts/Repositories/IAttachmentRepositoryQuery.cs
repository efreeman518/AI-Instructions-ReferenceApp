using EF.Common.Contracts;
using EF.Data.Contracts;
using TaskFlow.Application.Models;
using TaskFlow.Domain.Model;

namespace TaskFlow.Application.Contracts.Repositories;

public interface IAttachmentRepositoryQuery : IRepositoryBase
{
    Task<Attachment?> GetAttachmentAsync(Guid id, CancellationToken ct = default);
    Task<PagedResponse<Attachment>> SearchAttachmentsAsync(SearchRequest<AttachmentSearchFilter> request, CancellationToken ct = default);
}
