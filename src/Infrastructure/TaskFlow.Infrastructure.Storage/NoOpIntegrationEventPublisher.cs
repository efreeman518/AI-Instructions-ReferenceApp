using Microsoft.Extensions.Logging;
using TaskFlow.Application.Contracts.Messaging;

namespace TaskFlow.Infrastructure.Storage;

public class NoOpIntegrationEventPublisher(ILogger<NoOpIntegrationEventPublisher> logger) : IIntegrationEventPublisher
{
    public Task PublishAsync<TEvent>(TEvent integrationEvent, string? correlationId = null,
        CancellationToken ct = default) where TEvent : class
    {
        logger.LogDebug("NoOp: Would publish {EventType}", typeof(TEvent).Name);
        return Task.CompletedTask;
    }

    public Task PublishAsync<TEvent>(TEvent integrationEvent, string topicOrQueue,
        string? correlationId = null, CancellationToken ct = default) where TEvent : class
    {
        logger.LogDebug("NoOp: Would publish {EventType} to {TopicOrQueue}", typeof(TEvent).Name, topicOrQueue);
        return Task.CompletedTask;
    }
}