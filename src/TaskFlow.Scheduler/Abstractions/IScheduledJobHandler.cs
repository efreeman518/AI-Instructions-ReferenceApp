namespace TaskFlow.Scheduler.Abstractions;

public interface IScheduledJobHandler
{
    Task HandleAsync(CancellationToken ct);
}
