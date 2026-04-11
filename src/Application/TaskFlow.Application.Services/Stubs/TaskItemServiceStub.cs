using EF.Common.Contracts;
using TaskFlow.Application.Contracts.Services;
using TaskFlow.Application.Models;

namespace TaskFlow.Application.Services.Stubs;

public class TaskItemServiceStub : ITaskItemService
{
    public Task<Result<TaskItemDto>> CreateAsync(TaskItemDto dto, CancellationToken ct = default)
        => throw new NotImplementedException("Stub — implement in Phase 5b");

    public Task<Result<TaskItemDto>> UpdateAsync(TaskItemDto dto, CancellationToken ct = default)
        => throw new NotImplementedException("Stub — implement in Phase 5b");

    public Task<Result> DeleteAsync(Guid id, CancellationToken ct = default)
        => throw new NotImplementedException("Stub — implement in Phase 5b");

    public Task<Result<TaskItemDto>> GetAsync(Guid id, CancellationToken ct = default)
        => throw new NotImplementedException("Stub — implement in Phase 5b");

    public Task<Result<PagedResponse<TaskItemDto>>> SearchAsync(SearchRequest<TaskItemSearchFilter> request, CancellationToken ct = default)
        => throw new NotImplementedException("Stub — implement in Phase 5b");
}
