using Aspire.Hosting;
using Aspire.Hosting.Testing;
using AppHost;
using EF.IntegrationTesting.Aspire;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using EnvironmentVariableScope = EF.IntegrationTesting.Environment.EnvironmentVariableScope;
using FunctionsCoreToolsDiscovery = EF.IntegrationTesting.Environment.FunctionsCoreToolsDiscovery;

namespace Test.Aspire;

/// <summary>
/// Lazy assembly-scoped fixture that starts the full Aspire AppHost graph (API, Functions, SQL, Table
/// Storage) the first time a mesh test class calls <see cref="EnsureStartedAsync"/> from
/// <c>[ClassInitialize]</c>. Mesh tier (Aspire.Hosting.Testing) - the only tier that exercises the full
/// service mesh (HTTP -> API -> Service Bus -> Function -> projection -> audit row), which no lighter tier
/// reproduces. Teardown runs once via <c>AspireMeshLifecycle.[AssemblyCleanup]</c>. Per-call
/// <c>.WaitAsync(DefaultTimeout, ct)</c> bounds every async Aspire step; <c>WaitForResourceHealthyAsync</c>
/// avoids races where containers report Running before they accept connections.
/// </summary>
internal static class AspireTestHost
{
    /// <summary>
    /// Per-call deadline applied via <c>.WaitAsync(DefaultTimeout, ct)</c>. Bounds every async Aspire call
    /// (build, start, GetConnectionStringAsync, WaitForResource*) so a single hung step fails fast instead
    /// of hanging the whole test run. Sized for slow CI cold-starts (image pull + Functions warm-up).
    /// </summary>
    internal static readonly TimeSpan DefaultTimeout = TimeSpan.FromMinutes(5);

    /// <summary>Cleanup deadline. StopAsync should return promptly; the bound prevents a stuck shutdown.</summary>
    private static readonly TimeSpan CleanupTimeout = TimeSpan.FromMinutes(1);

    /// <summary>Guards the lazy single-start so concurrent <c>[ClassInitialize]</c> calls boot the graph once.</summary>
    private static readonly SemaphoreSlim Gate = new(1, 1);

    private static EnvironmentVariableScope? _environment;
    internal static string ConnectionString = null!;

    /// <summary>Shared Aspire app started once for all Aspire-based mesh tests.</summary>
    internal static DistributedApplication? AspireApp { get; private set; }

    /// <summary>
    /// Starts the Aspire graph on first call and returns immediately on subsequent calls. Mesh test classes
    /// call this from <c>[ClassInitialize]</c> so the ~60-90 s graph boot is paid only when a mesh test runs.
    /// </summary>
    internal static async Task EnsureStartedAsync(TestContext context)
    {
        if (AspireApp is not null)
            return;

        await Gate.WaitAsync(context.CancellationToken);
        try
        {
            if (AspireApp is not null)
                return;
            await StartAsync(context.CancellationToken);
        }
        finally
        {
            Gate.Release();
        }
    }

    private static async Task StartAsync(CancellationToken ct)
    {
        // AppHost.cs reads these via Environment.GetEnvironmentVariable, so they must be process env vars.
        _environment = new EnvironmentVariableScope()
            .Set("TASKFLOW_ASPIRE_TESTING", "true");

        if (EnsureFuncToolAvailable())
            _environment.Set("TASKFLOW_INCLUDE_FUNCTIONS", "true");

        var appHostProgramType = Type.GetType("Program, AppHost", throwOnError: true)!;

        var builder = await DistributedApplicationTestingBuilder.CreateAsync(
            appHostProgramType,
            args: [],
            configureBuilder: (appOptions, hostSettings) =>
            {
                // Default in the testing builder; setting explicitly to make intent visible.
                // Flip to false locally if you need to inspect resource state in the dashboard.
                appOptions.DisableDashboard = true;
                appOptions.EnableResourceLogging = true;

                // Pass the SQL parameter through host config instead of mutating Parameters__sql-password env var.
                // The AppHost's `builder.AddParameter("sql-password", ...)` resolves from IConfiguration first.
                hostSettings.Configuration ??= new();
                hostSettings.Configuration["Parameters:sql-password"] = LocalSqlSettings.SharedSaPassword;
            },
            cancellationToken: ct).WaitAsync(DefaultTimeout, ct);

        // Surface app-level diagnostics at Information while filtering out the noisy framework categories
        // (AspNetCore request logs, Aspire DCP/orchestration chatter). Drop the filters when debugging startup.
        builder.Services.AddLogging(logging =>
        {
            logging.SetMinimumLevel(LogLevel.Information);
            logging.AddFilter("Microsoft.AspNetCore", LogLevel.Warning);
            logging.AddFilter("Aspire.", LogLevel.Warning);
        });

        AspireApp = await builder.BuildAsync(ct).WaitAsync(DefaultTimeout, ct);
        await AspireApp.StartAsync(ct).WaitAsync(DefaultTimeout, ct);

        // Container reaching the Running state does not mean SQL is accepting connections - wait for the health check.
        await AspireApp.WaitForResourceHealthyAsync("taskflowdb", DefaultTimeout, ct);

        ConnectionString = await AspireApp.GetRequiredConnectionStringAsync("taskflowdb", DefaultTimeout, ct);
    }

    /// <summary>
    /// Stops and disposes the Aspire graph (if it was started) and restores mutated env vars. Invoked once
    /// by <c>AspireMeshLifecycle.[AssemblyCleanup]</c>, regardless of which mesh class warmed the graph up.
    /// </summary>
    internal static async Task StopAsync(CancellationToken ct)
    {
        if (AspireApp is not null)
        {
            try
            {
                await AspireApp.StopAsync(ct).WaitAsync(CleanupTimeout);
            }
            catch (TimeoutException)
            {
                // Bounded shutdown - DisposeAsync below still cleans up underlying processes/containers.
            }

            await AspireApp.DisposeAsync();
            AspireApp = null;
        }

        _environment?.Dispose();
        _environment = null;
    }

    /// <summary>
    /// Waits for a named Aspire resource to reach the Healthy state, bounded by <see cref="DefaultTimeout"/>.
    /// Tests should call this for any non-SQL resource (taskflowapi, taskflowfunctions, TableStorage1) before
    /// talking to it - Aspire reports Running before warm-up completes.
    /// </summary>
    internal static Task WaitForResourceHealthyAsync(string resourceName, CancellationToken cancellationToken = default)
    {
        if (AspireApp is null)
            throw new InvalidOperationException("AspireApp is not initialized.");

        return AspireApp.WaitForResourceHealthyAsync(resourceName, DefaultTimeout, cancellationToken);
    }

    /// <summary>
    /// Checks if Azure Functions Core Tools (func.exe) is available on PATH.
    /// Mutates PATH to include the discovery location on Windows if found via LocalAppData fallback.
    /// </summary>
    internal static bool EnsureFuncToolAvailable() => FunctionsCoreToolsDiscovery.EnsureFuncToolAvailable();
}
