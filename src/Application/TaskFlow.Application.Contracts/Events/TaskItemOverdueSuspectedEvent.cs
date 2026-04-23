namespace TaskFlow.Application.Contracts.Events;

public record TaskItemOverdueSuspectedEvent(Guid TaskItemId, Guid TenantId, DateTimeOffset DueDate);