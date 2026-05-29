namespace TaskFlow.Application.Contracts.Messaging;

/// <summary>Defines the integration event publisher contract used by TaskFlow components.</summary>
public interface IIntegrationEventPublisher
{
    /// <summary>Publishes publish through integration event publisher.</summary>
    Task PublishAsync<TEvent>(TEvent integrationEvent, string? correlationId = null,
        CancellationToken ct = default) where TEvent : class;

    /// <summary>Publishes publish through integration event publisher.</summary>
    Task PublishAsync<TEvent>(TEvent integrationEvent, string topicOrQueue,
        string? correlationId = null, CancellationToken ct = default) where TEvent : class;
}