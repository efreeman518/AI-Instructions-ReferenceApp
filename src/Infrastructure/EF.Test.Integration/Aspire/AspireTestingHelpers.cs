using Aspire.Hosting;
using Aspire.Hosting.Testing;

namespace EF.Test.Integration.Aspire;

public static class AspireTestingHelpers
{
    public static async Task WaitForResourceHealthyAsync(
        this DistributedApplication app,
        string resourceName,
        TimeSpan timeout,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(app);
        ArgumentException.ThrowIfNullOrWhiteSpace(resourceName);

        await app.ResourceNotifications
            .WaitForResourceHealthyAsync(resourceName, cancellationToken)
            .WaitAsync(timeout, cancellationToken);
    }

    public static async Task<string> GetRequiredConnectionStringAsync(
        this DistributedApplication app,
        string connectionName,
        TimeSpan timeout,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(app);
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionName);

        var connectionString = await app.GetConnectionStringAsync(connectionName, cancellationToken)
            .AsTask()
            .WaitAsync(timeout, cancellationToken);

        return string.IsNullOrWhiteSpace(connectionString)
            ? throw new InvalidOperationException($"Aspire connection string '{connectionName}' was not resolved.")
            : connectionString;
    }
}
