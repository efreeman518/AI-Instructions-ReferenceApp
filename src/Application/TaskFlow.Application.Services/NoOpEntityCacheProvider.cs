using TaskFlow.Application.Contracts;

namespace TaskFlow.Application.Services;

internal class NoOpEntityCacheProvider : IEntityCacheProvider
{
    public Task<T?> GetAsync<T>(string key, CancellationToken ct = default) where T : class
        => Task.FromResult<T?>(null);

    public Task SetAsync<T>(string key, T value, CancellationToken ct = default) where T : class
        => Task.CompletedTask;

    public Task RemoveAsync(string key, CancellationToken ct = default)
        => Task.CompletedTask;
}
