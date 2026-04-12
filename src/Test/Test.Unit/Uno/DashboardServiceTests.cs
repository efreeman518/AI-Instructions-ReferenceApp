using TaskFlow.Uno.Core.Business.Models;
using TaskFlow.Uno.Core.Business.Services;
using TaskFlow.Uno.Core.Client;

namespace Test.Unit.Uno;

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
        var taskService = new TaskItemApiService(apiClient);
        var dashboardService = new DashboardService(taskService);

        var summary = await dashboardService.GetSummaryAsync();

        Assert.AreEqual(3, summary.TotalTasks);
        Assert.AreEqual(1, summary.OpenTasks);       // Fix login validation
        Assert.AreEqual(1, summary.InProgressTasks);  // Build dashboard UI
        Assert.AreEqual(1, summary.CompletedTasks);   // Write documentation
        Assert.IsTrue(summary.OverdueTasks >= 1);     // Fix login validation is overdue
        Assert.IsTrue(summary.RecentActivity.Count > 0);
    }
}
