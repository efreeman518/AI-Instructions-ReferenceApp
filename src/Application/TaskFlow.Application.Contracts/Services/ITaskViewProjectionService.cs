namespace TaskFlow.Application.Contracts.Services;

/// <summary>Coordinates i task view projection application use cases with validation, tenant checks, repositories, and response shaping.</summary>
public interface ITaskViewProjectionService
{
    /// <summary>Provides the project task item operation for task view projection service.</summary>
    Task ProjectTaskItemAsync(Guid taskItemId, CancellationToken ct = default);
}
