using EF.Common.Contracts;
using TaskFlow.Application.Contracts.Services;
using TaskFlow.Application.Models;
using TaskFlow.Domain.Shared.Enums;
using TaskFlow.Scheduler.Abstractions;

namespace TaskFlow.Scheduler.Handlers;

public class OverdueTaskCheckHandler : IScheduledJobHandler
{
    private readonly ITaskItemService _taskItemService;
    private readonly ILogger<OverdueTaskCheckHandler> _logger;

    public OverdueTaskCheckHandler(ITaskItemService taskItemService, ILogger<OverdueTaskCheckHandler> logger)
    {
        _taskItemService = taskItemService;
        _logger = logger;
    }

    public async Task HandleAsync(CancellationToken ct)
    {
        _logger.LogInformation("Checking for overdue tasks...");

        var request = new SearchRequest<TaskItemSearchFilter>
        {
            PageSize = 500,
            PageIndex = 0,
            Filter = new TaskItemSearchFilter
            {
                IsOverdue = true
            }
        };

        var result = await _taskItemService.SearchAsync(request, ct);

        var overdueTasks = result.Data?
            .Where(t => t.Status != TaskItemStatus.Completed
                && t.Status != TaskItemStatus.Cancelled)
            .ToList() ?? [];

        _logger.LogInformation("Found {Count} overdue tasks", overdueTasks.Count);

        // Future: publish TaskItemOverdueSuspected domain events for notification/escalation
    }
}
