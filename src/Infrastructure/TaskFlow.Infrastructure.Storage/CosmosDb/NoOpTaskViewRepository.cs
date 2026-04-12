using Microsoft.Extensions.Logging;
using TaskFlow.Application.Contracts.Storage;

namespace TaskFlow.Infrastructure.Storage.CosmosDb;

public class NoOpTaskViewRepository(ILogger<NoOpTaskViewRepository> logger) : ITaskViewRepository
{
    public Task UpsertAsync(TaskViewDto taskView, CancellationToken ct = default)
    {
        logger.LogDebug("NoOp: Would upsert TaskView {Id}", taskView.Id);
        return Task.CompletedTask;
    }

    public Task<TaskViewDto?> GetAsync(string id, string tenantId, CancellationToken ct = default)
        => Task.FromResult<TaskViewDto?>(null);

    public Task<IReadOnlyList<TaskViewDto>> QueryByTenantAsync(string tenantId,
        int pageSize = 20, string? continuationToken = null, CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<TaskViewDto>>(Array.Empty<TaskViewDto>());

    public Task DeleteAsync(string id, string tenantId, CancellationToken ct = default)
        => Task.CompletedTask;
}
