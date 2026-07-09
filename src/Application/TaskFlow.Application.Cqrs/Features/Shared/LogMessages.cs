using Microsoft.Extensions.Logging;
using TaskFlow.Observability;

namespace TaskFlow.Application.Cqrs.Shared;

/// <summary>
/// Source-generated logging methods for the CQRS shared helpers. Using <see cref="LoggerMessageAttribute"/>
/// defers argument evaluation until the log level is enabled, satisfying CA1873 and avoiding needless work.
/// </summary>
internal static partial class LogMessages
{
    /// <summary>Logs that a search operation was cancelled by the client.</summary>
    [LoggerMessage(EventId = LogEventIds.ApplicationCqrsBase + 1, Level = LogLevel.Debug, Message = "{Operation} search cancelled by client.")]
    public static partial void SearchCancelled(this ILogger logger, string operation);
}
