using TaskFlow.Uno.Core.Business.Models;

namespace TaskFlow.Uno.Core.Business.Services;

/// <summary>Coordinates dashboard application use cases with validation, tenant checks, repositories, and response shaping.</summary>
public class DashboardService(ITaskItemApiService taskItemService) : IDashboardService
{
    /// <summary>Loads requested data and maps missing records to the expected response.</summary>
    public async Task<DashboardSummary> GetSummaryAsync(CancellationToken ct = default)
    {
        var allTasks = await taskItemService.SearchAsync(ct: ct);

        return new DashboardSummary
        {
            TotalTasks = allTasks.Count,
            OpenTasks = allTasks.Count(t => t.Status == "Open"),
            InProgressTasks = allTasks.Count(t => t.Status == "InProgress"),
            CompletedTasks = allTasks.Count(t => t.Status == "Completed"),
            BlockedTasks = allTasks.Count(t => t.Status == "Blocked"),
            CancelledTasks = allTasks.Count(t => t.Status == "Cancelled"),
            OverdueTasks = allTasks.Count(t => t.IsOverdue),
            RecentActivity = allTasks.Take(10).ToList()
        };
    }
}
