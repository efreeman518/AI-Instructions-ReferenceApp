using System.Text.Json;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Logging;
using TaskFlow.Application.Contracts.Messaging;

namespace TaskFlow.Infrastructure.Storage;

public class ServiceBusDomainEventPublisher : IDomainEventPublisher
{
    private readonly ServiceBusClient _client;
    private readonly ILogger<ServiceBusDomainEventPublisher> _logger;
    private const string DefaultTopic = "DomainEvents";

    public ServiceBusDomainEventPublisher(
        IAzureClientFactory<ServiceBusClient> clientFactory,
        ILogger<ServiceBusDomainEventPublisher> logger)
    {
        _client = clientFactory.CreateClient("TaskFlowSBClient");
        _logger = logger;
    }

    public Task PublishAsync<TEvent>(TEvent domainEvent, string? correlationId = null,
        CancellationToken ct = default) where TEvent : class
        => PublishAsync(domainEvent, DefaultTopic, correlationId, ct);

    public async Task PublishAsync<TEvent>(TEvent domainEvent, string topicOrQueue,
        string? correlationId = null, CancellationToken ct = default) where TEvent : class
    {
        await using var sender = _client.CreateSender(topicOrQueue);

        var message = new ServiceBusMessage(JsonSerializer.Serialize(domainEvent))
        {
            ContentType = "application/json",
            Subject = typeof(TEvent).Name,
            MessageId = Guid.NewGuid().ToString()
        };

        if (correlationId is not null)
            message.CorrelationId = correlationId;

        message.ApplicationProperties["EventType"] = typeof(TEvent).Name;
        message.ApplicationProperties["PublishedAt"] = DateTimeOffset.UtcNow.ToString("O");

        await sender.SendMessageAsync(message, ct);
        _logger.LogInformation("Published {EventType} to {TopicOrQueue}", typeof(TEvent).Name, topicOrQueue);
    }
}
