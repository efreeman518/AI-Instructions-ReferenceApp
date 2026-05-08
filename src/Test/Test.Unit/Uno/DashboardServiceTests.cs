using Moq;
using TaskFlow.Uno.Core.Business.Models;
using TaskFlow.Uno.Core.Business.Notifications;
using TaskFlow.Uno.Core.Business.Services;
using TaskFlow.Uno.Core.Client;

namespace Test.Unit.Uno;

/// <summary>
/// Validates <c>DashboardService.GetSummaryAsync</c> aggregates the mocked task data (totals by status,
/// overdue count, recent activity) when wired over the in-process <c>MockHttpMessageHandler</c>.
/// Pure-unit tier: in-memory composition; no real server, no real DB.
/// </summary>
[TestClass]
[TestCategory("Unit")]
[TestCategory("Uno")]
public class DashboardServiceTests
{
    [TestMethod]
    public async Task GetSummaryAsync_AggregatesMockData()
    {
        using var handler = new MockHttpMessageHandler();
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://localhost:7200") };
        var apiClient = new TaskFlowApiClient(httpClient);
        var taskService = new TaskItemApiService(apiClient, Mock.Of<INotificationService>());
        var dashboardService = new DashboardService(taskService);

        var summary = await dashboardService.GetSummaryAsync();

        Assert.AreEqual(14, summary.TotalTasks);
        Assert.AreEqual(7, summary.OpenTasks);
        Assert.AreEqual(3, summary.InProgressTasks);
        Assert.AreEqual(2, summary.CompletedTasks);
        Assert.AreEqual(1, summary.BlockedTasks);
        Assert.AreEqual(1, summary.CancelledTasks);
        // Signature is IsGreaterThanOrEqualTo(lowerBound, value) — asserts value >= lowerBound.
        Assert.IsGreaterThanOrEqualTo(1, summary.OverdueTasks);     // at least "Fix login validation" is overdue
        Assert.IsNotEmpty(summary.RecentActivity);
    }
}
