namespace TaskFlow.Bootstrapper;

/// <summary>Defines the startup task contract used by TaskFlow components.</summary>
public interface IStartupTask
{
    /// <summary>Provides the execute operation for startup task.</summary>
    Task ExecuteAsync(CancellationToken cancellationToken = default);
}
