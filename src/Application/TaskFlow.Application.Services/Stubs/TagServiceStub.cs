using EF.Common.Contracts;
using TaskFlow.Application.Contracts.Services;
using TaskFlow.Application.Models;

namespace TaskFlow.Application.Services.Stubs;

public class TagServiceStub : ITagService
{
    public Task<Result<TagDto>> CreateAsync(TagDto dto, CancellationToken ct = default)
        => throw new NotImplementedException("Stub — implement in Phase 5b");

    public Task<Result<TagDto>> UpdateAsync(TagDto dto, CancellationToken ct = default)
        => throw new NotImplementedException("Stub — implement in Phase 5b");

    public Task<Result> DeleteAsync(Guid id, CancellationToken ct = default)
        => throw new NotImplementedException("Stub — implement in Phase 5b");

    public Task<Result<TagDto>> GetAsync(Guid id, CancellationToken ct = default)
        => throw new NotImplementedException("Stub — implement in Phase 5b");

    public Task<Result<PagedResponse<TagDto>>> SearchAsync(SearchRequest<TagSearchFilter> request, CancellationToken ct = default)
        => throw new NotImplementedException("Stub — implement in Phase 5b");
}
