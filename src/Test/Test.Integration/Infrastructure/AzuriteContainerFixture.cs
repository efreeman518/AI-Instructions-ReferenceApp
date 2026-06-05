using Testcontainers.Azurite;

namespace Test.Integration.Infrastructure;

/// <summary>
/// Standalone Azurite Testcontainer for the component tier. Provides a real Table Storage endpoint for
/// the audit-repository test without booting the Aspire AppHost graph. Started once by
/// <see cref="IntegrationTestSetup"/>; <see cref="StartupError"/> is captured (not thrown) so a container
/// failure marks only the dependent tests Inconclusive instead of aborting the whole assembly.
/// </summary>
internal static class AzuriteContainerFixture
{
    // Pin the image explicitly - the parameterless AzuriteBuilder() ctor is obsolete in Testcontainers.Azurite 4.x.
    private static readonly AzuriteContainer Azurite =
        new AzuriteBuilder("mcr.microsoft.com/azure-storage/azurite:3.33.0").Build();

    /// <summary>Startup failure captured by <see cref="StartAsync"/>; null when the container started cleanly.</summary>
    internal static Exception? StartupError { get; private set; }

    /// <summary>Azurite connection string (blob/queue/table). Only valid once startup succeeded.</summary>
    internal static string ConnectionString => Azurite.GetConnectionString();

    /// <summary>Starts the Azurite container, capturing any failure for the Inconclusive-on-failure pattern.</summary>
    internal static async Task StartAsync()
    {
        try
        {
            await Azurite.StartAsync();
        }
        catch (Exception ex)
        {
            StartupError = ex;
        }
    }

    /// <summary>Disposes the Azurite container.</summary>
    internal static async Task StopAsync() => await Azurite.DisposeAsync();
}
