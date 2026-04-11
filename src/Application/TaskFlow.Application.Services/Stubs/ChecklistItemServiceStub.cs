using EF.Common.Contracts;
using TaskFlow.Application.Contracts.Services;
using TaskFlow.Application.Models;

namespace TaskFlow.Application.Services.Stubs;

public class ChecklistItemServiceStub : IChecklistItemService
{
    public Task<Result<ChecklistItemDto>> CreateAsync(ChecklistItemDto dto, CancellationToken ct = default)
        => throw new NotImplementedException("Stub — implement in Phase 5b");

    public Task<Result<ChecklistItemDto>> UpdateAsync(ChecklistItemDto dto, CancellationToken ct = default)
        => throw new NotImplementedException("Stub — implement in Phase 5b");

    public Task<Result> DeleteAsync(Guid id, CancellationToken ct = default)
        => throw new NotImplementedException("Stub — implement in Phase 5b");

    public Task<Result<ChecklistItemDto>> GetAsync(Guid id, CancellationToken ct = default)
        => throw new NotImplementedException("Stub — implement in Phase 5b");

    public Task<Result<PagedResponse<ChecklistItemDto>>> SearchAsync(SearchRequest<ChecklistItemSearchFilter> request, CancellationToken ct = default)
        => throw new NotImplementedException("Stub — implement in Phase 5b");
}
