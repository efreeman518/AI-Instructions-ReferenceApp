namespace TaskFlow.Application.Contracts.Events;

/// <summary>Provides task item created event behavior for the Application Events layer.</summary>
public record TaskItemCreatedEvent(Guid TaskItemId, Guid TenantId, string Title);