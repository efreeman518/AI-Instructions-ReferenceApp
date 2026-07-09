using EF.Common.Contracts;
using TaskFlow.Application.Contracts.Services;
using TaskFlow.Application.Models;
using TaskFlow.Domain.Shared.Enums;
using TaskFlow.Scheduler.Abstractions;

namespace TaskFlow.Scheduler.Handlers;

/// <summary>Handles stale task cleanup work by coordinating validation, tenant boundaries, persistence, and response mapping.</summary>
public class StaleTaskCleanupHandler : IScheduledJobHandler
{
    private readonly ITaskItemService _taskItemService;
    private readonly ILogger<StaleTaskCleanupHandler> _logger;

    /// <summary>Initializes stale task cleanup handler with required dependencies and default state.</summary>
    public StaleTaskCleanupHandler(ITaskItemService taskItemService, ILogger<StaleTaskCleanupHandler> logger)
    {
        _taskItemService = taskItemService;
        _logger = logger;
    }

    /// <summary>Handles stale task cleanup requests and returns the application result.</summary>
    public async Task HandleAsync(CancellationToken ct)
    {
        _logger.LogInformation("Cleaning up stale tasks...");

        var request = new SearchRequest<TaskItemSearchFilter>
        {
            PageSize = 500,
            PageIndex = 0,
            Filter = new TaskItemSearchFilter
            {
                Status = TaskItemStatus.Cancelled
            }
        };

        var result = await _taskItemService.SearchAsync(request, ct);

        var staleDays = 90;
        var staleTasks = result.Data?
            .Where(t => t.DueDate.HasValue
                && t.DueDate.Value < DateTimeOffset.UtcNow.AddDays(-staleDays))
            .ToList() ?? [];

        _logger.StaleTasksFound(staleTasks.Count, staleDays);

        // Future: archive or soft-delete stale tasks
    }
}
