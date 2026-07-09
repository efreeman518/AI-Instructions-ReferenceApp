using Microsoft.Extensions.Logging;
using TaskFlow.Observability;

namespace TaskFlow.Scheduler;

/// <summary>
/// Source-generated logging methods for the Scheduler host. Using <see cref="LoggerMessageAttribute"/>
/// defers argument evaluation until the log level is enabled, satisfying CA1873 and avoiding needless work.
/// </summary>
internal static partial class LogMessages
{
    /// <summary>Logs that a scheduled job is starting.</summary>
    [LoggerMessage(EventId = LogEventIds.SchedulerBase + 1, Level = LogLevel.Information, Message = "Job {JobName} starting at {UtcNow}")]
    public static partial void JobStarting(this ILogger logger, string jobName, DateTime utcNow);

    /// <summary>Logs that a scheduled job completed.</summary>
    [LoggerMessage(EventId = LogEventIds.SchedulerBase + 2, Level = LogLevel.Information, Message = "Job {JobName} completed in {ElapsedMs}ms")]
    public static partial void JobCompleted(this ILogger logger, string jobName, long elapsedMs);

    /// <summary>Logs the number of overdue tasks found.</summary>
    [LoggerMessage(EventId = LogEventIds.SchedulerBase + 3, Level = LogLevel.Information, Message = "Found {Count} overdue tasks")]
    public static partial void OverdueTasksFound(this ILogger logger, int count);

    /// <summary>Logs the number of recurring task templates found.</summary>
    [LoggerMessage(EventId = LogEventIds.SchedulerBase + 4, Level = LogLevel.Information, Message = "Found {Count} recurring task templates to evaluate")]
    public static partial void RecurringTemplatesFound(this ILogger logger, int count);

    /// <summary>Logs the number of stale tasks found.</summary>
    [LoggerMessage(EventId = LogEventIds.SchedulerBase + 5, Level = LogLevel.Information, Message = "Found {Count} stale tasks (cancelled > {StaleDays} days ago)")]
    public static partial void StaleTasksFound(this ILogger logger, int count, int staleDays);
}
