namespace TaskFlow.Scheduler.Abstractions;

/// <summary>Handles i scheduled job work by coordinating validation, tenant boundaries, persistence, and response mapping.</summary>
public interface IScheduledJobHandler
{
    /// <summary>Handles i scheduled job requests and returns the application result.</summary>
    Task HandleAsync(CancellationToken ct);
}
