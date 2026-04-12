namespace TaskFlow.Application.Contracts.Messaging;

public interface IDomainEventPublisher
{
    Task PublishAsync<TEvent>(TEvent domainEvent, string? correlationId = null,
        CancellationToken ct = default) where TEvent : class;

    Task PublishAsync<TEvent>(TEvent domainEvent, string topicOrQueue,
        string? correlationId = null, CancellationToken ct = default) where TEvent : class;
}
