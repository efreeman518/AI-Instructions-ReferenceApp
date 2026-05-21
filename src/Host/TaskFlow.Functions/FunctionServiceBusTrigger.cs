using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using TaskFlow.Application.Contracts.Services;

namespace TaskFlow.Functions;

public class FunctionServiceBusTrigger(
    ILogger<FunctionServiceBusTrigger> logger,
    ITaskViewProjectionService projectionService)
{
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
