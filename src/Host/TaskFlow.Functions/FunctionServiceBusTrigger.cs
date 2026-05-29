using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using TaskFlow.Application.Contracts.Services;

namespace TaskFlow.Functions;

/// <summary>
/// Service Bus projection trigger. It consumes application integration events from the
/// DomainEvents topic and updates read models through application services.
/// </summary>
public class FunctionServiceBusTrigger(
    ILogger<FunctionServiceBusTrigger> logger,
    ITaskViewProjectionService projectionService)
{
    /// <summary>
    /// Dispatches task-related events by message Subject. Unknown event types are logged because
    /// the topic can contain future integration events that this Function version does not handle.
    /// </summary>
    [Function(nameof(ProcessTaskEvent))]
    public async Task ProcessTaskEvent(
        [ServiceBusTrigger("%DomainEventsTopic%", "%DomainEventsSubscription%",
            Connection = "ServiceBus1", IsSessionsEnabled = false)]
        string messageBody,
        FunctionContext context,
        CancellationToken ct)
    {
        var eventType = "Unknown";
        if (context.BindingContext.BindingData.TryGetValue("Subject", out var subjectObj))
            eventType = subjectObj?.ToString() ?? eventType;

        logger.LogInformation("Service Bus trigger: received {EventType}, length {Length}",
            eventType, messageBody.Length);

        switch (eventType)
        {
            case "TaskItemCreatedEvent":
            case "TaskItemStatusChangedEvent":
                var taskItemId = ExtractTaskItemId(messageBody);
                if (taskItemId.HasValue)
                    await projectionService.ProjectTaskItemAsync(taskItemId.Value, ct);
                break;
            default:
                logger.LogWarning("Unknown event type: {EventType}", eventType);
                break;
        }
    }

    /// <summary>Extracts task item ID from the supplied message or payload.</summary>
    private static Guid? ExtractTaskItemId(string messageBody)
    {
        try
        {
            using var doc = JsonDocument.Parse(messageBody);
            if (doc.RootElement.TryGetProperty("TaskItemId", out var prop))
                return prop.GetGuid();
        }
        catch { }
        return null;
    }
}
