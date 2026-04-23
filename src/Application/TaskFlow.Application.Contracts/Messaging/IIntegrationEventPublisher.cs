namespace TaskFlow.Application.Contracts.Messaging;

public interface IIntegrationEventPublisher
{
    Task PublishAsync<TEvent>(TEvent integrationEvent, string? correlationId = null,
        CancellationToken ct = default) where TEvent : class;

    Task PublishAsync<TEvent>(TEvent integrationEvent, string topicOrQueue,
        string? correlationId = null, CancellationToken ct = default) where TEvent : class;
}