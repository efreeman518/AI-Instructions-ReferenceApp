using TaskFlow.Domain.Shared;

namespace TaskFlow.Domain.Model.Events;

public record CommentAddedEvent(Guid CommentId, Guid TaskItemId, Guid TenantId) : IDomainEvent;
