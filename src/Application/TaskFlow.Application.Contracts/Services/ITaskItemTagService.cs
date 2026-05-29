using EF.Common.Contracts;
using TaskFlow.Application.Models;

namespace TaskFlow.Application.Contracts.Services;

/// <summary>Coordinates i task item tag application use cases with validation, tenant checks, repositories, and response shaping.</summary>
public interface ITaskItemTagService
{
    /// <summary>Loads requested data and maps missing records to the expected response.</summary>
    Task<Result<DefaultResponse<TaskItemTagDto>>> GetAsync(Guid id, CancellationToken ct = default);
    /// <summary>Creates requested data after validation and maps the result to the caller contract.</summary>
    Task<Result<DefaultResponse<TaskItemTagDto>>> CreateAsync(DefaultRequest<TaskItemTagDto> request, CancellationToken ct = default);
    /// <summary>Deletes requested data and maps failures to the caller contract.</summary>
    Task<Result> DeleteAsync(Guid id, CancellationToken ct = default);
}
