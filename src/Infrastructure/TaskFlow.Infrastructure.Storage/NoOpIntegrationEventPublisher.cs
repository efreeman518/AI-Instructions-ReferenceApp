using Microsoft.Extensions.Logging;
using TaskFlow.Application.Contracts.Messaging;

namespace TaskFlow.Infrastructure.Storage;

/// <summary>Provides no op integration event publisher behavior for the Infrastructure layer.</summary>
public class NoOpIntegrationEventPublisher(ILogger<NoOpIntegrationEventPublisher> logger) : IIntegrationEventPublisher
{
    /// <summary>Publishes publish through no op integration event publisher.</summary>
    public Task PublishAsync<TEvent>(TEvent integrationEvent, string? correlationId = null,
        CancellationToken ct = default) where TEvent : class
    {
        logger.NoOpPublish(typeof(TEvent).Name);
        return Task.CompletedTask;
    }

    /// <summary>Publishes publish through no op integration event publisher.</summary>
    public Task PublishAsync<TEvent>(TEvent integrationEvent, string topicOrQueue,
        string? correlationId = null, CancellationToken ct = default) where TEvent : class
    {
        logger.NoOpPublishTo(typeof(TEvent).Name, topicOrQueue);
        return Task.CompletedTask;
    }
}
