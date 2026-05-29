namespace TaskFlow.Application.Contracts.Events;

/// <summary>Provides comment added event behavior for the Application Events layer.</summary>
public record CommentAddedEvent(Guid CommentId, Guid TaskItemId, Guid TenantId);