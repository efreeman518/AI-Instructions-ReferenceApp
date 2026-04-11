using EF.Common.Contracts;
using TaskFlow.Application.Models;

namespace TaskFlow.Application.Contracts.Services;

public interface IAttachmentService
{
    Task<Result<AttachmentDto>> CreateAsync(AttachmentDto dto, CancellationToken ct = default);
    Task<Result<AttachmentDto>> UpdateAsync(AttachmentDto dto, CancellationToken ct = default);
    Task<Result> DeleteAsync(Guid id, CancellationToken ct = default);
    Task<Result<AttachmentDto>> GetAsync(Guid id, CancellationToken ct = default);
    Task<Result<PagedResponse<AttachmentDto>>> SearchAsync(SearchRequest<AttachmentSearchFilter> request, CancellationToken ct = default);
}
