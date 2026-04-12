using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace TaskFlow.Functions;

public class FunctionTimerTrigger(ILogger<FunctionTimerTrigger> logger)
{
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
