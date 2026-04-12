namespace TaskFlow.Application.Contracts.Services;

public interface ITaskViewProjectionService
{
    Task ProjectTaskItemAsync(Guid taskItemId, CancellationToken ct = default);
}
