using TaskFlow.Uno.Core.Business.Models;

namespace TaskFlow.Uno.Core.Business.Services;

public class DashboardService(ITaskItemApiService taskItemService) : IDashboardService
{
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
            RecentActivity = allTasks.OrderByDescending(t => t.Id).Take(10).ToList()
        };
    }
}
