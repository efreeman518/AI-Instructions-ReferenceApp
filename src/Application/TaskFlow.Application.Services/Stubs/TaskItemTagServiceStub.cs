using EF.Common.Contracts;
using TaskFlow.Application.Contracts.Services;
using TaskFlow.Application.Models;

namespace TaskFlow.Application.Services.Stubs;

public class TaskItemTagServiceStub : ITaskItemTagService
{
    public Task<Result<TaskItemTagDto>> CreateAsync(TaskItemTagDto dto, CancellationToken ct = default)
        => throw new NotImplementedException("Stub — implement in Phase 5b");

    public Task<Result> DeleteAsync(Guid id, CancellationToken ct = default)
        => throw new NotImplementedException("Stub — implement in Phase 5b");

    public Task<Result<TaskItemTagDto>> GetAsync(Guid id, CancellationToken ct = default)
        => throw new NotImplementedException("Stub — implement in Phase 5b");
}
