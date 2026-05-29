using TickerQ.Utilities.Base;
using TaskFlow.Scheduler.Handlers;
using TaskFlow.Scheduler.Telemetry;

namespace TaskFlow.Scheduler.Jobs;

/// <summary>Configures task maintenance jobs host behavior for TaskFlow runtime services.</summary>
public class TaskMaintenanceJobs : BaseTickerQJob
{
    /// <summary>Initializes task maintenance jobs with required dependencies and default state.</summary>
    public TaskMaintenanceJobs(
        IServiceScopeFactory scopeFactory,
        ILogger<TaskMaintenanceJobs> logger,
        SchedulingMetrics metrics)
        : base(scopeFactory, logger, metrics) { }

    /// <summary>Provides the overdue task check operation for task maintenance jobs.</summary>
    [TickerFunction("OverdueTaskCheck")]
    public async Task OverdueTaskCheckAsync(TickerFunctionContext context, CancellationToken ct)
    {
        await ExecuteJobAsync<OverdueTaskCheckHandler>("OverdueTaskCheck", context, ct);
    }

    /// <summary>Provides the recurring task generation operation for task maintenance jobs.</summary>
    [TickerFunction("RecurringTaskGeneration")]
    public async Task RecurringTaskGenerationAsync(TickerFunctionContext context, CancellationToken ct)
    {
        await ExecuteJobAsync<RecurringTaskGenerationHandler>("RecurringTaskGeneration", context, ct);
    }

    /// <summary>Provides the stale task cleanup operation for task maintenance jobs.</summary>
    [TickerFunction("StaleTaskCleanup")]
    public async Task StaleTaskCleanupAsync(TickerFunctionContext context, CancellationToken ct)
    {
        await ExecuteJobAsync<StaleTaskCleanupHandler>("StaleTaskCleanup", context, ct);
    }
}
