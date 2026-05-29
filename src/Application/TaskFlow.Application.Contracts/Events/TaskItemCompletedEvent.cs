namespace TaskFlow.Application.Contracts.Events;

/// <summary>Provides task item completed event behavior for the Application Events layer.</summary>
public record TaskItemCompletedEvent(Guid TaskItemId, Guid TenantId, DateTimeOffset CompletedDate);