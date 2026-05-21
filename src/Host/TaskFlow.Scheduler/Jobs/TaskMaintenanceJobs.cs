using TickerQ.Utilities.Base;
using TaskFlow.Scheduler.Handlers;
using TaskFlow.Scheduler.Telemetry;

namespace TaskFlow.Scheduler.Jobs;

public class TaskMaintenanceJobs : BaseTickerQJob
{
    public TaskMaintenanceJobs(
        IServiceScopeFactory scopeFactory,
        ILogger<TaskMaintenanceJobs> logger,
        SchedulingMetrics metrics)
        : base(scopeFactory, logger, metrics) { }

    [TickerFunction("OverdueTaskCheck")]
    public async Task OverdueTaskCheckAsync(TickerFunctionContext context, CancellationToken ct)
    {
        await ExecuteJobAsync<OverdueTaskCheckHandler>("OverdueTaskCheck", context, ct);
    }

    [TickerFunction("RecurringTaskGeneration")]
    public async Task RecurringTaskGenerationAsync(TickerFunctionContext context, CancellationToken ct)
    {
        await ExecuteJobAsync<RecurringTaskGenerationHandler>("RecurringTaskGeneration", context, ct);
    }

    [TickerFunction("StaleTaskCleanup")]
    public async Task StaleTaskCleanupAsync(TickerFunctionContext context, CancellationToken ct)
    {
        await ExecuteJobAsync<StaleTaskCleanupHandler>("StaleTaskCleanup", context, ct);
    }
}
