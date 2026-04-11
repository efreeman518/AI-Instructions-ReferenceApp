using EF.Common.Contracts;
using TaskFlow.Application.Models;

namespace TaskFlow.Application.Contracts.Services;

public interface IAttachmentService
{
    Task<PagedResponse<AttachmentDto>> SearchAsync(SearchRequest<AttachmentSearchFilter> request, CancellationToken ct = default);
    Task<Result<DefaultResponse<AttachmentDto>>> GetAsync(Guid id, CancellationToken ct = default);
    Task<Result<DefaultResponse<AttachmentDto>>> CreateAsync(DefaultRequest<AttachmentDto> request, CancellationToken ct = default);
    Task<Result<DefaultResponse<AttachmentDto>>> UpdateAsync(DefaultRequest<AttachmentDto> request, CancellationToken ct = default);
    Task<Result> DeleteAsync(Guid id, CancellationToken ct = default);
}
