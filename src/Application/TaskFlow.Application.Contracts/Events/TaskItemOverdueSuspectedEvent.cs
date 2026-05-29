namespace TaskFlow.Application.Contracts.Events;

/// <summary>Provides task item overdue suspected event behavior for the Application Events layer.</summary>
public record TaskItemOverdueSuspectedEvent(Guid TaskItemId, Guid TenantId, DateTimeOffset DueDate);