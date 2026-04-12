using EF.Common.Contracts;
using TaskFlow.Application.Contracts.Services;
using TaskFlow.Application.Models;
using TaskFlow.Domain.Shared.Enums;
using TaskFlow.Scheduler.Abstractions;

namespace TaskFlow.Scheduler.Handlers;

public class StaleTaskCleanupHandler : IScheduledJobHandler
{
    private readonly ITaskItemService _taskItemService;
    private readonly ILogger<StaleTaskCleanupHandler> _logger;

    public StaleTaskCleanupHandler(ITaskItemService taskItemService, ILogger<StaleTaskCleanupHandler> logger)
    {
        _taskItemService = taskItemService;
        _logger = logger;
    }

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

        _logger.LogInformation("Found {Count} stale tasks (cancelled > {StaleDays} days ago)", staleTasks.Count, staleDays);

        // Future: archive or soft-delete stale tasks
    }
}
