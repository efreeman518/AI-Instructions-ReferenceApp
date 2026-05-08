using Aspire.Hosting;
using Aspire.Hosting.Testing;
using AppHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Test.Integration;

/// <summary>
/// Assembly-scoped fixture that starts the full Aspire AppHost graph (API, Functions, SQL, Table Storage)
/// once for the test run via <c>[AssemblyInitialize]</c> and tears it down via <c>[AssemblyCleanup]</c>.
/// Aspire tier (Aspire.Hosting.Testing) — required so downstream classes can exercise the full service
/// mesh (HTTP → API → Service Bus → Function → projection → audit row), which no lighter tier reproduces.
/// Per-call <c>.WaitAsync(DefaultTimeout, ct)</c> bounds every async Aspire step;
/// <c>WaitForResourceHealthyAsync</c> avoids races where containers report Running before they accept
/// connections.
/// </summary>
[TestClass]
public class AspireTestHost
{
    /// <summary>
    /// Per-call deadline applied via <c>.WaitAsync(DefaultTimeout, ct)</c>. Bounds every async Aspire call
    /// (build, start, GetConnectionStringAsync, WaitForResource*) so a single hung step fails fast instead
    /// of hanging the whole test run. Sized for slow CI cold-starts (image pull + Functions warm-up).
    /// </summary>
    internal static readonly TimeSpan DefaultTimeout = TimeSpan.FromMinutes(5);

    /// <summary>Cleanup deadline. StopAsync should return promptly; the bound prevents a stuck shutdown.</summary>
    private static readonly TimeSpan CleanupTimeout = TimeSpan.FromMinutes(1);

    private static string? _originalAspireTesting;
    private static string? _originalIncludeFunctions;
    internal static string ConnectionString = null!;

    /// <summary>Shared Aspire app started once for all Aspire-based integration tests.</summary>
    internal static DistributedApplication? AspireApp { get; private set; }

    [AssemblyInitialize]
    public static async Task AssemblyInit(TestContext _)
    {
        // AppHost/Program.cs reads these via Environment.GetEnvironmentVariable, so they must be process env vars.
        _originalAspireTesting = Environment.GetEnvironmentVariable("TASKFLOW_ASPIRE_TESTING");
        _originalIncludeFunctions = Environment.GetEnvironmentVariable("TASKFLOW_INCLUDE_FUNCTIONS");
        Environment.SetEnvironmentVariable("TASKFLOW_ASPIRE_TESTING", "true");
        if (EnsureFuncToolAvailable())
            Environment.SetEnvironmentVariable("TASKFLOW_INCLUDE_FUNCTIONS", "true");

        var ct = CancellationToken.None;

        var appHostProgramType = Type.GetType("Program, AppHost", throwOnError: true)!;

        var builder = await DistributedApplicationTestingBuilder.CreateAsync(
            appHostProgramType,
            args: [],
            configureBuilder: (appOptions, hostSettings) =>
            {
                // Default in the testing builder; setting explicitly to make intent visible.
                // Flip to false locally if you need to inspect resource state in the dashboard.
                appOptions.DisableDashboard = true;

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

        // Container reaching the Running state does not mean SQL is accepting connections — wait for the health check.
        // Without this, the first test using ConnectionString races SQL warm-up.
        await AspireApp.ResourceNotifications.WaitForResourceHealthyAsync("taskflowdb", ct)
            .WaitAsync(DefaultTimeout, ct);

        // GetConnectionStringAsync returns ValueTask; convert to Task to apply WaitAsync.
        var sqlConnectionString = await AspireApp.GetConnectionStringAsync("taskflowdb", ct)
            .AsTask()
            .WaitAsync(DefaultTimeout, ct);
        ConnectionString = string.IsNullOrWhiteSpace(sqlConnectionString)
            ? throw new InvalidOperationException("Aspire SQL connection string 'taskflowdb' was not resolved.")
            : sqlConnectionString;
    }

    [AssemblyCleanup]
    public static async Task AssemblyCleanup(TestContext testContext)
    {
        if (AspireApp is not null)
        {
            try
            {
                await AspireApp.StopAsync(testContext.CancellationToken).WaitAsync(CleanupTimeout);
            }
            catch (TimeoutException)
            {
                // Bounded shutdown — DisposeAsync below still cleans up underlying processes/containers.
            }
            await AspireApp.DisposeAsync();
        }

        Environment.SetEnvironmentVariable("TASKFLOW_ASPIRE_TESTING", _originalAspireTesting);
        Environment.SetEnvironmentVariable("TASKFLOW_INCLUDE_FUNCTIONS", _originalIncludeFunctions);
    }

    /// <summary>
    /// Waits for a named Aspire resource to reach the Healthy state, bounded by <see cref="DefaultTimeout"/>.
    /// Tests should call this for any non-SQL resource (taskflowapi, taskflowfunctions, TableStorage1)
    /// before talking to it — Aspire reports Running before warm-up completes.
    /// </summary>
    internal static Task WaitForResourceHealthyAsync(string resourceName, CancellationToken cancellationToken = default)
    {
        if (AspireApp is null)
            throw new InvalidOperationException("AspireApp is not initialized.");

        return AspireApp.ResourceNotifications
            .WaitForResourceHealthyAsync(resourceName, cancellationToken)
            .WaitAsync(DefaultTimeout, cancellationToken);
    }

    /// <summary>
    /// Checks if Azure Functions Core Tools (func.exe) is available on PATH.
    /// Mutates PATH to include the discovery location on Windows if found via LocalAppData fallback.
    /// </summary>
    internal static bool EnsureFuncToolAvailable()
    {
        var candidateNames = OperatingSystem.IsWindows()
            ? new[] { "func.exe", "func.cmd", "func.bat" }
            : new[] { "func" };

        var path = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
        var pathEntries = path.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        foreach (var directory in pathEntries)
            foreach (var candidate in candidateNames)
                if (File.Exists(Path.Combine(directory, candidate)))
                    return true;

        if (!OperatingSystem.IsWindows())
            return false;

        var localFunctionsToolsRoot = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "AzureFunctionsTools",
            "Releases");

        if (!Directory.Exists(localFunctionsToolsRoot))
            return false;

        var discoveredDirectory = Directory
            .EnumerateFiles(localFunctionsToolsRoot, "func.exe", SearchOption.AllDirectories)
            .Select(Path.GetDirectoryName)
            .Where(directory => !string.IsNullOrWhiteSpace(directory))
            .Select(directory => directory!)
            .OrderByDescending(directory => directory.Contains("4.", StringComparison.OrdinalIgnoreCase))
            .ThenByDescending(directory => directory, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault();

        if (discoveredDirectory == null)
            return false;

        Environment.SetEnvironmentVariable(
            "PATH",
            string.IsNullOrWhiteSpace(path)
                ? discoveredDirectory
                : string.Join(Path.PathSeparator, [discoveredDirectory, .. pathEntries]));

        return true;
    }
}
