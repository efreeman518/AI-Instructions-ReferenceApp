using Microsoft.Extensions.Logging;
using TaskFlow.Observability;

namespace TaskFlow.Infrastructure.Storage;

/// <summary>
/// Source-generated logging methods for the Infrastructure.Storage layer. Using <see cref="LoggerMessageAttribute"/>
/// defers argument evaluation until the log level is enabled, satisfying CA1873 and avoiding needless work.
/// </summary>
internal static partial class LogMessages
{
    /// <summary>Logs that an audit entry was persisted to the audit store.</summary>
    [LoggerMessage(EventId = LogEventIds.InfrastructureStorageBase + 1, Level = LogLevel.Information, Message = "Persisted audit entry {AuditEntryId} for tenant {TenantId} entity {EntityType} action {Action}")]
    public static partial void AuditEntryPersisted(this ILogger logger, Guid auditEntryId, string? tenantId, string entityType, string action);

    /// <summary>Logs that a TaskView read model was upserted.</summary>
    [LoggerMessage(EventId = LogEventIds.InfrastructureStorageBase + 2, Level = LogLevel.Debug, Message = "Upserted TaskView {Id} for tenant {TenantId}")]
    public static partial void TaskViewUpserted(this ILogger logger, string id, string tenantId);

    /// <summary>Logs that a TaskView was not found during deletion.</summary>
    [LoggerMessage(EventId = LogEventIds.InfrastructureStorageBase + 3, Level = LogLevel.Debug, Message = "TaskView {Id} not found for deletion")]
    public static partial void TaskViewNotFoundForDeletion(this ILogger logger, string id);

    /// <summary>Logs a no-op TaskView upsert.</summary>
    [LoggerMessage(EventId = LogEventIds.InfrastructureStorageBase + 4, Level = LogLevel.Debug, Message = "NoOp: Would upsert TaskView {Id}")]
    public static partial void NoOpTaskViewUpsert(this ILogger logger, string id);

    /// <summary>Logs a no-op audit entry persist.</summary>
    [LoggerMessage(EventId = LogEventIds.InfrastructureStorageBase + 5, Level = LogLevel.Debug, Message = "NoOp: would persist audit entry {AuditEntryId}")]
    public static partial void NoOpAuditPersist(this ILogger logger, Guid auditEntryId);

    /// <summary>Logs a no-op integration event publish.</summary>
    [LoggerMessage(EventId = LogEventIds.InfrastructureStorageBase + 6, Level = LogLevel.Debug, Message = "NoOp: Would publish {EventType}")]
    public static partial void NoOpPublish(this ILogger logger, string eventType);

    /// <summary>Logs a no-op integration event publish to a specific destination.</summary>
    [LoggerMessage(EventId = LogEventIds.InfrastructureStorageBase + 7, Level = LogLevel.Debug, Message = "NoOp: Would publish {EventType} to {TopicOrQueue}")]
    public static partial void NoOpPublishTo(this ILogger logger, string eventType, string topicOrQueue);

    /// <summary>Logs that an integration event was published to Service Bus.</summary>
    [LoggerMessage(EventId = LogEventIds.InfrastructureStorageBase + 8, Level = LogLevel.Information, Message = "Published {EventType} to {TopicOrQueue}")]
    public static partial void EventPublished(this ILogger logger, string eventType, string topicOrQueue);
}
