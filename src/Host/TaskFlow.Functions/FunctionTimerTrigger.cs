using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace TaskFlow.Functions;

/// <summary>Configures function timer trigger host behavior for TaskFlow runtime services.</summary>
public class FunctionTimerTrigger(ILogger<FunctionTimerTrigger> logger)
{
    /// <summary>Provides the stale task cleanup operation for function timer trigger.</summary>
    [Function(nameof(StaleTaskCleanup))]
    public Task StaleTaskCleanup(
        [TimerTrigger("%StaleTaskCleanupCron%")] TimerInfo timer,
        CancellationToken ct)
    {
        logger.LogInformation("StaleTaskCleanup timer fired at {UtcNow}. Next: {NextRun}",
            DateTime.UtcNow, timer.ScheduleStatus?.Next);

        // Future: wire to StaleTaskCleanupHandler or ITaskItemService
        return Task.CompletedTask;
    }
}
