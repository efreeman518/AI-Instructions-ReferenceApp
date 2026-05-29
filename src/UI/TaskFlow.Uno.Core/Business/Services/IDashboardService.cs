using TaskFlow.Uno.Core.Business.Models;

namespace TaskFlow.Uno.Core.Business.Services;

/// <summary>Coordinates i dashboard application use cases with validation, tenant checks, repositories, and response shaping.</summary>
public interface IDashboardService
{
    /// <summary>Loads requested data and maps missing records to the expected response.</summary>
    Task<DashboardSummary> GetSummaryAsync(CancellationToken ct = default);
}
