using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace TaskFlow.Functions;

public class FunctionServiceBusTrigger(ILogger<FunctionServiceBusTrigger> logger)
{
    [Function(nameof(ProcessTaskEvent))]
    public Task ProcessTaskEvent(
        [ServiceBusTrigger("%DomainEventsTopic%", "%DomainEventsSubscription%",
            Connection = "ServiceBusConnection", IsSessionsEnabled = false)]
        string messageBody,
        CancellationToken ct)
    {
        logger.LogInformation("Service Bus trigger: received domain event, length {Length}",
            messageBody.Length);

        // Future: deserialize domain event and route to appropriate handler
        // e.g., TaskItemCreated → update Cosmos projection, send notification
        return Task.CompletedTask;
    }
}
