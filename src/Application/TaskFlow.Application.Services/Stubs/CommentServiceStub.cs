using EF.Common.Contracts;
using TaskFlow.Application.Contracts.Services;
using TaskFlow.Application.Models;

namespace TaskFlow.Application.Services.Stubs;

public class CommentServiceStub : ICommentService
{
    public Task<Result<CommentDto>> CreateAsync(CommentDto dto, CancellationToken ct = default)
        => throw new NotImplementedException("Stub — implement in Phase 5b");

    public Task<Result<CommentDto>> UpdateAsync(CommentDto dto, CancellationToken ct = default)
        => throw new NotImplementedException("Stub — implement in Phase 5b");

    public Task<Result> DeleteAsync(Guid id, CancellationToken ct = default)
        => throw new NotImplementedException("Stub — implement in Phase 5b");

    public Task<Result<CommentDto>> GetAsync(Guid id, CancellationToken ct = default)
        => throw new NotImplementedException("Stub — implement in Phase 5b");

    public Task<Result<PagedResponse<CommentDto>>> SearchAsync(SearchRequest<CommentSearchFilter> request, CancellationToken ct = default)
        => throw new NotImplementedException("Stub — implement in Phase 5b");
}
