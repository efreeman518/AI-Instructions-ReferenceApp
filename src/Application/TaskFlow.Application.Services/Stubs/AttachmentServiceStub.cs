using EF.Common.Contracts;
using TaskFlow.Application.Contracts.Services;
using TaskFlow.Application.Models;

namespace TaskFlow.Application.Services.Stubs;

public class AttachmentServiceStub : IAttachmentService
{
    public Task<Result<AttachmentDto>> CreateAsync(AttachmentDto dto, CancellationToken ct = default)
        => throw new NotImplementedException("Stub — implement in Phase 5b");

    public Task<Result<AttachmentDto>> UpdateAsync(AttachmentDto dto, CancellationToken ct = default)
        => throw new NotImplementedException("Stub — implement in Phase 5b");

    public Task<Result> DeleteAsync(Guid id, CancellationToken ct = default)
        => throw new NotImplementedException("Stub — implement in Phase 5b");

    public Task<Result<AttachmentDto>> GetAsync(Guid id, CancellationToken ct = default)
        => throw new NotImplementedException("Stub — implement in Phase 5b");

    public Task<Result<PagedResponse<AttachmentDto>>> SearchAsync(SearchRequest<AttachmentSearchFilter> request, CancellationToken ct = default)
        => throw new NotImplementedException("Stub — implement in Phase 5b");
}
