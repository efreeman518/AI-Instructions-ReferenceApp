using System.Collections.Concurrent;
using TaskFlow.Scheduler.Telemetry;
using TickerQ.Utilities.Enums;
using TickerQ.Utilities.Interfaces;

namespace TaskFlow.Scheduler.Infrastructure;

public sealed class TaskFlowSchedulerExceptionHandler(
    ILogger<TaskFlowSchedulerExceptionHandler> logger,
    SchedulingMetrics metrics) : ITickerExceptionHandler
{
    private static readonly ConcurrentDictionary<Guid, string> JobNameCache = new();

    public static void RegisterJobName(Guid tickerId, string jobName)
    {
        JobNameCache.TryAdd(tickerId, jobName);
    }

    public static void UnregisterJobName(Guid tickerId)
    {
        JobNameCache.TryRemove(tickerId, out _);
    }

    public Task HandleExceptionAsync(Exception exception, Guid tickerId, TickerType tickerType)
    {
        var jobName = ResolveJobName(tickerId);
        logger.LogError(exception,
            "Scheduler job failed. JobName: {JobName}, TickerId: {TickerId}, TickerType: {TickerType}",
            jobName,
            tickerId,
            tickerType);

        metrics.RecordJobFailure(jobName, exception.Message);
        UnregisterJobName(tickerId);
        return Task.CompletedTask;
    }

    public Task HandleCanceledExceptionAsync(Exception exception, Guid tickerId, TickerType tickerType)
    {
        var jobName = ResolveJobName(tickerId);
        logger.LogWarning(
            "Scheduler job cancelled. JobName: {JobName}, TickerId: {TickerId}, TickerType: {TickerType}, Reason: {Reason}",
            jobName,
            tickerId,
            tickerType,
            exception.Message);

        UnregisterJobName(tickerId);
        return Task.CompletedTask;
    }

    private static string ResolveJobName(Guid tickerId) =>
        JobNameCache.TryGetValue(tickerId, out var jobName)
            ? jobName
            : $"Unknown-{tickerId:N}";
}
