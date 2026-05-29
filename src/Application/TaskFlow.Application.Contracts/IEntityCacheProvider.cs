namespace TaskFlow.Application.Contracts;

/// <summary>Defines the entity cache provider contract used by TaskFlow components.</summary>
public interface IEntityCacheProvider
{
    /// <summary>Loads requested data and maps missing records to the expected response.</summary>
    Task<T?> GetAsync<T>(string key, CancellationToken ct = default) where T : class;
    /// <summary>Provides the set operation for entity cache provider.</summary>
    Task SetAsync<T>(string key, T value, CancellationToken ct = default) where T : class;
    /// <summary>Removes remove while keeping aggregate relationship state consistent.</summary>
    Task RemoveAsync(string key, CancellationToken ct = default);
}
