namespace TaskFlow.Domain.Shared;

/// <summary>
/// Marker interface for domain events. Bridged to EF.BackgroundServices.IMessage
/// in the Infrastructure layer during Phase 5b.
/// </summary>
public interface IDomainEvent;
