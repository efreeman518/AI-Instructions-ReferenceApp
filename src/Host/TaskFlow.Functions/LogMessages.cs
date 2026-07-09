using Microsoft.Extensions.Logging;
using TaskFlow.Observability;

namespace TaskFlow.Functions;

/// <summary>
/// Source-generated logging methods for the Functions host. Using <see cref="LoggerMessageAttribute"/>
/// defers argument evaluation until the log level is enabled, satisfying CA1873 and avoiding needless work.
/// </summary>
internal static partial class LogMessages
{
    /// <summary>Logs that a blob-trigger attachment is being processed.</summary>
    [LoggerMessage(EventId = LogEventIds.FunctionsBase + 1, Level = LogLevel.Information, Message = "Blob trigger: processing attachment '{Name}', size {Size} bytes")]
    public static partial void BlobProcessing(this ILogger logger, string name, long size);

    /// <summary>Logs that a category was created via the category trigger.</summary>
    [LoggerMessage(EventId = LogEventIds.FunctionsBase + 2, Level = LogLevel.Information, Message = "CreateCategory created {CategoryId}")]
    public static partial void CategoryCreated(this ILogger logger, Guid? categoryId);

    /// <summary>Logs that a health check was requested.</summary>
    [LoggerMessage(EventId = LogEventIds.FunctionsBase + 3, Level = LogLevel.Information, Message = "Health check requested at {UtcNow}")]
    public static partial void HealthCheckRequested(this ILogger logger, DateTime utcNow);

    /// <summary>Logs that the task API proxy was invoked.</summary>
    [LoggerMessage(EventId = LogEventIds.FunctionsBase + 4, Level = LogLevel.Information, Message = "TaskApiProxy invoked at {UtcNow}")]
    public static partial void TaskApiProxyInvoked(this ILogger logger, DateTime utcNow);

    /// <summary>Logs that a Service Bus message was received by the projection trigger.</summary>
    [LoggerMessage(EventId = LogEventIds.FunctionsBase + 5, Level = LogLevel.Information, Message = "Service Bus trigger: received {EventType}, length {Length}")]
    public static partial void ServiceBusReceived(this ILogger logger, string eventType, int length);

    /// <summary>Logs that the stale-task-cleanup timer fired.</summary>
    [LoggerMessage(EventId = LogEventIds.FunctionsBase + 6, Level = LogLevel.Information, Message = "StaleTaskCleanup timer fired at {UtcNow}. Next: {NextRun}")]
    public static partial void StaleTaskCleanupTimerFired(this ILogger logger, DateTime utcNow, DateTimeOffset? nextRun);
}
