using System.Text.Json;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Logging;
using TaskFlow.Application.Contracts.Messaging;

namespace TaskFlow.Infrastructure.Storage;

public class ServiceBusIntegrationEventPublisher : IIntegrationEventPublisher
{
    private readonly ServiceBusClient _client;
    private readonly ILogger<ServiceBusIntegrationEventPublisher> _logger;
    private const string DefaultTopic = "DomainEvents";

    public ServiceBusIntegrationEventPublisher(
        IAzureClientFactory<ServiceBusClient> clientFactory,
        ILogger<ServiceBusIntegrationEventPublisher> logger)
    {
        _client = clientFactory.CreateClient("TaskFlowSBClient");
        _logger = logger;
    }

    public Task PublishAsync<TEvent>(TEvent integrationEvent, string? correlationId = null,
        CancellationToken ct = default) where TEvent : class
        => PublishAsync(integrationEvent, DefaultTopic, correlationId, ct);

    public async Task PublishAsync<TEvent>(TEvent integrationEvent, string topicOrQueue,
        string? correlationId = null, CancellationToken ct = default) where TEvent : class
    {
        await using var sender = _client.CreateSender(topicOrQueue);

        var message = new ServiceBusMessage(JsonSerializer.Serialize(integrationEvent))
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