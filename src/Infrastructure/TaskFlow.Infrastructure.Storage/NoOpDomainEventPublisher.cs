using Microsoft.Extensions.Logging;
using TaskFlow.Application.Contracts.Messaging;

namespace TaskFlow.Infrastructure.Storage;

public class NoOpDomainEventPublisher(ILogger<NoOpDomainEventPublisher> logger) : IDomainEventPublisher
{
    public Task PublishAsync<TEvent>(TEvent domainEvent, string? correlationId = null,
        CancellationToken ct = default) where TEvent : class
    {
        logger.LogDebug("NoOp: Would publish {EventType}", typeof(TEvent).Name);
        return Task.CompletedTask;
    }

    public Task PublishAsync<TEvent>(TEvent domainEvent, string topicOrQueue,
        string? correlationId = null, CancellationToken ct = default) where TEvent : class
    {
        logger.LogDebug("NoOp: Would publish {EventType} to {TopicOrQueue}", typeof(TEvent).Name, topicOrQueue);
        return Task.CompletedTask;
    }
}
