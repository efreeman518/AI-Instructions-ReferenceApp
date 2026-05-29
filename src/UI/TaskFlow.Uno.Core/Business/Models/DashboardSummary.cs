namespace TaskFlow.Uno.Core.Business.Models;

/// <summary>Carries dashboard summary data between Uno services and presentation models.</summary>
public record DashboardSummary
{
    public int TotalTasks { get; init; }
    public int OpenTasks { get; init; }
    public int InProgressTasks { get; init; }
    public int CompletedTasks { get; init; }
    public int BlockedTasks { get; init; }
    public int CancelledTasks { get; init; }
    public int OverdueTasks { get; init; }
    public IReadOnlyList<TaskItemModel> RecentActivity { get; init; } = [];
}
