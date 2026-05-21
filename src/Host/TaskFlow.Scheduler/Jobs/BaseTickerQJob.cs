using System.Diagnostics;
using TaskFlow.Scheduler.Abstractions;
using TaskFlow.Scheduler.Infrastructure;
using TaskFlow.Scheduler.Telemetry;
using TickerQ.Utilities.Base;

namespace TaskFlow.Scheduler.Jobs;

public abstract class BaseTickerQJob
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger _logger;
    private readonly SchedulingMetrics _metrics;

    protected BaseTickerQJob(IServiceScopeFactory scopeFactory, ILogger logger, SchedulingMetrics metrics)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _metrics = metrics;
    }

    protected async Task ExecuteJobAsync<THandler>(
        string jobName, TickerFunctionContext context, CancellationToken ct)
        where THandler : IScheduledJobHandler
    {
        var sw = Stopwatch.StartNew();
        TaskFlowSchedulerExceptionHandler.RegisterJobName(context.Id, jobName);
        _logger.LogInformation("Job {JobName} starting at {UtcNow}", jobName, DateTime.UtcNow);

        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var handler = scope.ServiceProvider.GetRequiredService<THandler>();
            await handler.HandleAsync(ct);

            sw.Stop();
            _metrics.RecordJobSuccess(jobName, sw.Elapsed.TotalMilliseconds);
            _logger.LogInformation("Job {JobName} completed in {ElapsedMs}ms", jobName, sw.ElapsedMilliseconds);
            TaskFlowSchedulerExceptionHandler.UnregisterJobName(context.Id);
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogError(ex, "Job {JobName} failed after {ElapsedMs}ms", jobName, sw.ElapsedMilliseconds);
            throw;
        }
    }
}
