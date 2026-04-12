using EF.Common.Contracts;
using TaskFlow.Application.Contracts.Services;
using TaskFlow.Application.Models;
using TaskFlow.Scheduler.Abstractions;

namespace TaskFlow.Scheduler.Handlers;

public class RecurringTaskGenerationHandler : IScheduledJobHandler
{
    private readonly ITaskItemService _taskItemService;
    private readonly ILogger<RecurringTaskGenerationHandler> _logger;

    public RecurringTaskGenerationHandler(ITaskItemService taskItemService, ILogger<RecurringTaskGenerationHandler> logger)
    {
        _taskItemService = taskItemService;
        _logger = logger;
    }

    public async Task HandleAsync(CancellationToken ct)
    {
        _logger.LogInformation("Generating recurring tasks...");

        var request = new SearchRequest<TaskItemSearchFilter>
        {
            PageSize = 500,
            PageIndex = 0,
            Filter = new TaskItemSearchFilter()
        };

        var result = await _taskItemService.SearchAsync(request, ct);

        var recurringTasks = result.Data?
            .Where(t => !string.IsNullOrEmpty(t.RecurrenceFrequency))
            .ToList() ?? [];

        _logger.LogInformation("Found {Count} recurring task templates to evaluate", recurringTasks.Count);

        // Future: evaluate recurrence patterns and create new task instances
    }
}
