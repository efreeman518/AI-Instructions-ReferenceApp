using TaskFlow.Application.Contracts;

namespace TaskFlow.Application.Services;

/// <summary>Provides no op entity cache provider behavior for the Application layer.</summary>
internal class NoOpEntityCacheProvider : IEntityCacheProvider
{
    /// <summary>Loads requested data and maps missing records to the expected response.</summary>
    public Task<T?> GetAsync<T>(string key, CancellationToken ct = default) where T : class
        => Task.FromResult<T?>(null);

    /// <summary>Provides the set operation for no op entity cache provider.</summary>
    public Task SetAsync<T>(string key, T value, CancellationToken ct = default) where T : class
        => Task.CompletedTask;

    /// <summary>Removes remove while keeping aggregate relationship state consistent.</summary>
    public Task RemoveAsync(string key, CancellationToken ct = default)
        => Task.CompletedTask;
}
