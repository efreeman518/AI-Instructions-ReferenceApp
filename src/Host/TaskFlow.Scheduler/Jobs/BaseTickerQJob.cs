using System.Diagnostics;
using TaskFlow.Scheduler.Abstractions;
using TickerQ.Utilities.Base;

namespace TaskFlow.Scheduler.Jobs;

public abstract class BaseTickerQJob
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger _logger;

    protected BaseTickerQJob(IServiceScopeFactory scopeFactory, ILogger logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected async Task ExecuteJobAsync<THandler>(
        string jobName, TickerFunctionContext context, CancellationToken ct)
        where THandler : IScheduledJobHandler
    {
        var sw = Stopwatch.StartNew();
        _logger.LogInformation("Job {JobName} starting at {UtcNow}", jobName, DateTime.UtcNow);

        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var handler = scope.ServiceProvider.GetRequiredService<THandler>();
            await handler.HandleAsync(ct);

            sw.Stop();
            _logger.LogInformation("Job {JobName} completed in {ElapsedMs}ms", jobName, sw.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogError(ex, "Job {JobName} failed after {ElapsedMs}ms", jobName, sw.ElapsedMilliseconds);
            throw;
        }
    }
}
