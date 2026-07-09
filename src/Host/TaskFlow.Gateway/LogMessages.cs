using Microsoft.Extensions.Logging;
using TaskFlow.Observability;

namespace TaskFlow.Gateway;

/// <summary>
/// Source-generated logging methods for the Gateway. Using <see cref="LoggerMessageAttribute"/>
/// defers argument evaluation until the log level is enabled, satisfying CA1873 and avoiding needless work.
/// </summary>
internal static partial class LogMessages
{
    /// <summary>Logs that a token was acquired for a downstream cluster.</summary>
    [LoggerMessage(EventId = LogEventIds.GatewayBase + 1, Level = LogLevel.Debug, Message = "Token acquired for cluster {ClusterId}, expires {Expiry}")]
    public static partial void TokenAcquired(this ILogger logger, string clusterId, DateTimeOffset expiry);

    /// <summary>Logs that a scaffold stub token was issued for a downstream cluster.</summary>
    [LoggerMessage(EventId = LogEventIds.GatewayBase + 2, Level = LogLevel.Debug, Message = "Scaffold token issued for cluster {ClusterId}, expires {Expiry}")]
    public static partial void ScaffoldTokenIssued(this ILogger logger, string clusterId, DateTimeOffset expiry);
}
