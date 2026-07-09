using Microsoft.Extensions.Logging;
using TaskFlow.Observability;

namespace TaskFlow.Application.Services;

/// <summary>
/// Source-generated logging methods for the Application.Services layer. Using
/// <see cref="LoggerMessageAttribute"/> defers argument evaluation until the log level is enabled,
/// satisfying CA1873 and avoiding needless work.
/// </summary>
internal static partial class LogMessages
{
    /// <summary>Logs that a TaskItem was projected to a TaskView read model.</summary>
    [LoggerMessage(EventId = LogEventIds.ApplicationServicesBase + 1, Level = LogLevel.Information, Message = "Projected TaskItem {Id} to TaskView")]
    public static partial void TaskItemProjected(this ILogger logger, Guid id);
}
