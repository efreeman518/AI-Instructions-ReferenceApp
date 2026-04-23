namespace TaskFlow.Application.Contracts.Events;

public record CommentAddedEvent(Guid CommentId, Guid TaskItemId, Guid TenantId);