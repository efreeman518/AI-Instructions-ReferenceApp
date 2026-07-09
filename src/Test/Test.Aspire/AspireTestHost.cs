using AppHost;
using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Testing;
using EF.IntegrationTesting.Aspire;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Net.Sockets;
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

    internal const string ResourceLoggingEnvironmentVariable = "TASKFLOW_ASPIRE_RESOURCE_LOGGING";
    internal const string FoundryLocalOptInEnvironmentVariable = "TASKFLOW_ASPIRE_ENABLE_FOUNDRY_LOCAL";

    /// <summary>Guards the lazy single-start so concurrent <c>[ClassInitialize]</c> calls boot the graph once.</summary>
    private static readonly SemaphoreSlim Gate = new(1, 1);

    private static EnvironmentVariableScope? _environment;
    internal static string ConnectionString = null!;

    /// <summary>Shared Aspire app started once for all Aspire-based mesh tests.</summary>
    internal static DistributedApplication? AspireApp { get; private set; }

    /// <summary>AI provider the shared Aspire graph exposed through AppHost configuration.</summary>
    internal static AspireAiProvider AiProvider { get; private set; } = AspireAiProvider.None;

    /// <summary>True when the React Vite project can run from this checkout.</summary>
    internal static bool ReactAvailable { get; private set; }

    /// <summary>True when built Uno WASM assets are present for the static host.</summary>
    internal static bool UnoWasmAvailable { get; private set; }

    /// <summary>True only when the internal Aspire diagnostic logging switch is enabled.</summary>
    internal static bool ResourceLoggingEnabled { get; private set; }

    /// <summary>
    /// Starts the Aspire graph on first call and returns immediately on subsequent calls. Mesh test classes
    /// call this from <c>[ClassInitialize]</c> so the ~60-90 s graph boot is paid only when a mesh test runs.
    /// </summary>
    internal static async Task EnsureStartedAsync(TestContext context)
    {
        if (AspireApp is not null)
            return;

        await Gate.WaitAsync(context.CancellationToken);
        string? unavailableReason = null;
        try
        {
            if (AspireApp is not null)
                return;

            try
            {
                await StartAsync(context.CancellationToken);
            }
            catch (Exception ex) when (ShouldTreatAsUnavailableResource(ex))
            {
                await StopAsync(CancellationToken.None);
                unavailableReason = $"Aspire app host could not start because a required resource or dependency is unavailable. {ex.Message}";
            }
            catch
            {
                await StopAsync(CancellationToken.None);
                throw;
            }
        }
        finally
        {
            Gate.Release();
        }

        if (unavailableReason is not null)
        {
            Assert.Inconclusive(unavailableReason);
        }
    }

    private static async Task StartAsync(CancellationToken ct)
    {
        // AppHost.cs reads these via Environment.GetEnvironmentVariable, so they must be process env vars.
        _environment = new EnvironmentVariableScope()
            .Set("TASKFLOW_ASPIRE_TESTING", "true")
            .Set(FoundryLocalOptInEnvironmentVariable, "false");

        if (EnsureFuncToolAvailable())
            _environment.Set("TASKFLOW_ASPIRE_FUNCTIONS_AVAILABLE", "true");

        ReactAvailable = IsReactRunnable();
        if (ReactAvailable)
            _environment.Set("TASKFLOW_ASPIRE_REACT_AVAILABLE", "true");

        UnoWasmAvailable = IsUnoWasmRunnable();
        if (UnoWasmAvailable)
            _environment.Set("TASKFLOW_ASPIRE_UNO_WASM_AVAILABLE", "true");

        ResourceLoggingEnabled = IsEnabled(ResourceLoggingEnvironmentVariable);
        var appHostProgramType = Type.GetType("Program, AppHost", throwOnError: true)!;

        var builder = await DistributedApplicationTestingBuilder.CreateAsync(
            appHostProgramType,
            args: [],
            configureBuilder: (appOptions, hostSettings) =>
            {
                appOptions.DisableDashboard = true;
                // Keep normal Aspire test output quiet. Set TASKFLOW_ASPIRE_RESOURCE_LOGGING=true
                // only while diagnosing resource startup or routing failures.
                appOptions.EnableResourceLogging = ResourceLoggingEnabled;

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
            logging.ClearProviders();
            logging.AddConsole();
            logging.SetMinimumLevel(LogLevel.Information);
            logging.AddFilter("Microsoft.AspNetCore", LogLevel.Warning);
            logging.AddFilter("Aspire.", LogLevel.Warning);
        });

        AspireApp = await builder.BuildAsync(ct).WaitAsync(DefaultTimeout, ct);
        await AspireApp.StartAsync(ct).WaitAsync(DefaultTimeout, ct);
        AiProvider = SelectRequestedAiProviderForTesting();

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
                await AspireApp.StopAsync(ct).WaitAsync(CleanupTimeout, ct);
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
        AiProvider = AspireAiProvider.None;
        ReactAvailable = false;
        UnoWasmAvailable = false;
        ResourceLoggingEnabled = false;
    }

    /// <summary>
    /// Waits for a named Aspire resource to reach the Healthy state, bounded by <see cref="DefaultTimeout"/>.
    /// Tests should call this for any non-SQL resource (taskflowapi, taskflowfunctions, TableStorage1) before
    /// talking to it - Aspire reports Running before warm-up completes.
    /// </summary>
    internal static async Task WaitForResourceHealthyAsync(string resourceName, CancellationToken cancellationToken = default)
    {
        if (AspireApp is null)
            throw new InvalidOperationException("AspireApp is not initialized.");

        string? unhealthyReason = null;
        try
        {
            await AspireApp.WaitForResourceHealthyAsync(resourceName, DefaultTimeout, cancellationToken);
        }
        catch (Exception ex) when (ShouldTreatAsUnavailableResource(ex))
        {
            await DumpResourceDiagnosticsAsync(resourceName, cancellationToken);
            await DumpResourceDiagnosticsAsync("taskflowmigrator", cancellationToken);

            var state = AspireApp.ResourceNotifications.TryGetCurrentState(resourceName, out var resourceEvent)
                ? resourceEvent.Snapshot.State?.Text
                : "unavailable";

            unhealthyReason =
                $"Aspire resource '{resourceName}' did not become healthy. Current state: {state}. This usually indicates that a required local resource or dependency is unavailable in this environment. {ex.Message}";
        }
        catch
        {
            await DumpResourceDiagnosticsAsync(resourceName, cancellationToken);
            await DumpResourceDiagnosticsAsync("taskflowmigrator", cancellationToken);
            throw;
        }

        if (unhealthyReason is not null)
        {
            Assert.Inconclusive(unhealthyReason);
        }
    }

    internal static async Task<bool> TryAssertInconclusiveForUnavailableResourcesAsync(
        Exception ex,
        CancellationToken cancellationToken = default,
        params string[] resourceNames)
    {
        if (AspireApp is null)
            return false;

        var affectedResources = GetUnavailableResourceSummaries(resourceNames);
        if (affectedResources.Count == 0)
        {
            if (!LooksLikeUnavailableEndpointException(ex))
                return false;

            affectedResources.Add($"{string.Join(", ", resourceNames)}: endpoint unavailable");
        }

        foreach (var resourceName in resourceNames.Distinct(StringComparer.OrdinalIgnoreCase))
            await DumpResourceDiagnosticsAsync(resourceName, cancellationToken);

        Assert.Inconclusive(
            $"Required local resources appear unavailable for this test. {string.Join("; ", affectedResources)}. Exception: {ex.GetType().Name}: {ex.Message}");

        return true;
    }

    private static bool ShouldTreatAsUnavailableResource(Exception ex) =>
        ex is DistributedApplicationException
        || ex is TimeoutException
        || ex is OperationCanceledException
        || string.Equals(ex.GetType().Name, "TimeoutRejectedException", StringComparison.Ordinal);

    private static List<string> GetUnavailableResourceSummaries(IEnumerable<string> resourceNames)
    {
        if (AspireApp is null)
            return [];

        List<string> affectedResources = [];

        foreach (var resourceName in resourceNames.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            if (!AspireApp.ResourceNotifications.TryGetCurrentState(resourceName, out var resourceEvent))
            {
                affectedResources.Add($"{resourceName}: state unavailable");
                continue;
            }

            var snapshot = resourceEvent.Snapshot;
            var state = snapshot.State?.Text ?? "unknown";
            var health = snapshot.HealthStatus?.ToString() ?? "unknown";

            if (string.Equals(state, "Finished", StringComparison.OrdinalIgnoreCase)
                && snapshot.ExitCode is null or 0)
            {
                continue;
            }

            if (snapshot.ExitCode is int exitCode && exitCode != 0)
            {
                affectedResources.Add($"{resourceName}: state={state}, health={health}, exit={exitCode}");
                continue;
            }

            if (!string.Equals(health, "Healthy", StringComparison.OrdinalIgnoreCase))
            {
                affectedResources.Add($"{resourceName}: state={state}, health={health}");
                continue;
            }

            if (state is "Failed" or "Exited" or "Stopped")
                affectedResources.Add($"{resourceName}: state={state}, health={health}");
        }

        return affectedResources;
    }

    private static bool LooksLikeUnavailableEndpointException(Exception ex)
    {
        for (var current = ex; current is not null; current = current.InnerException)
        {
            if (current is HttpRequestException or IOException or SocketException or TimeoutException)
                return true;

            var message = current.Message;
            if (message.Contains("forcibly closed by the remote host", StringComparison.OrdinalIgnoreCase)
                || message.Contains("response ended prematurely", StringComparison.OrdinalIgnoreCase)
                || message.Contains("while sending the request", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static async Task DumpResourceDiagnosticsAsync(string resourceName, CancellationToken cancellationToken)
    {
        if (AspireApp is null)
            return;

        if (AspireApp.ResourceNotifications.TryGetCurrentState(resourceName, out var resourceEvent))
        {
            Console.WriteLine(
                $"{resourceName}: state={resourceEvent.Snapshot.State?.Text}, health={resourceEvent.Snapshot.HealthStatus}");
        }
        else
        {
            Console.WriteLine($"{resourceName}: no resource state available.");
        }

        var logs = AspireApp.Services.GetService<ResourceLoggerService>();
        if (logs is null)
        {
            Console.WriteLine(
                $"{resourceName}: resource logging disabled; set {ResourceLoggingEnvironmentVariable}=true to capture Aspire resource logs.");
            return;
        }

        try
        {
            await foreach (var batch in logs.GetAllAsync(resourceName).WithCancellation(cancellationToken))
            {
                foreach (var line in batch)
                    Console.WriteLine($"{resourceName}: {line}");
            }
        }
        catch (InvalidOperationException ex)
        {
            Console.WriteLine($"{resourceName}: resource logs unavailable: {ex.Message}");
        }
    }

    /// <summary>
    /// Checks if Azure Functions Core Tools (func.exe) is available on PATH.
    /// Mutates PATH to include the discovery location on Windows if found via LocalAppData fallback.
    /// </summary>
    internal static bool EnsureFuncToolAvailable() => FunctionsCoreToolsDiscovery.EnsureFuncToolAvailable();

    internal static AspireAiProvider SelectRequestedAiProviderForTesting()
    {
        return IsAzureFoundryRequested()
            ? AspireAiProvider.AzureFoundry
            : IsFoundryLocalRequested()
                ? AspireAiProvider.FoundryLocal
            : AspireAiProvider.None;
    }

    private static bool IsAzureFoundryRequested()
    {
        return IsEnabled("TASKFLOW_USE_AZURE_FOUNDRY")
            || HasValue("AiServices__FoundryEndpoint")
            || HasValue("AiServices:FoundryEndpoint");
    }

    private static bool IsFoundryLocalRequested() => IsEnabled(FoundryLocalOptInEnvironmentVariable);

    private static bool IsEnabled(string variableName) =>
        string.Equals(Environment.GetEnvironmentVariable(variableName), "true", StringComparison.OrdinalIgnoreCase);

    private static bool HasValue(string variableName) =>
        !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(variableName));

    private static bool CommandSucceeds(string fileName, string arguments)
    {
        try
        {
            using var process = Process.Start(new ProcessStartInfo(fileName, arguments)
            {
                CreateNoWindow = true,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                UseShellExecute = false
            });

            if (process is null)
                return false;

            if (!process.WaitForExit((int)TimeSpan.FromSeconds(10).TotalMilliseconds))
            {
                try
                {
                    process.Kill(entireProcessTree: true);
                }
                catch (InvalidOperationException)
                {
                }

                return false;
            }

            return process.ExitCode == 0;
        }
        catch (Exception ex) when (ex is InvalidOperationException or System.ComponentModel.Win32Exception)
        {
            return false;
        }
    }

    private static bool IsReactRunnable()
    {
        var repoRoot = FindRepoRoot();
        if (repoRoot is null)
            return false;

        var reactRoot = Path.Combine(repoRoot, "src", "UI", "TaskFlow.React");
        var viteShim = OperatingSystem.IsWindows()
            ? Path.Combine(reactRoot, "node_modules", ".bin", "vite.cmd")
            : Path.Combine(reactRoot, "node_modules", ".bin", "vite");

        return File.Exists(Path.Combine(reactRoot, "package.json"))
            && File.Exists(viteShim)
            && CommandSucceeds("node", "--version");
    }

    private static bool IsUnoWasmRunnable()
    {
        var repoRoot = FindRepoRoot();
        if (repoRoot is null)
            return false;

        var outputRoot = Path.Combine(repoRoot, "src", "UI", "TaskFlow.Uno", "bin");
        if (!Directory.Exists(outputRoot))
            return false;

        return Directory.EnumerateFiles(outputRoot, "index.html", SearchOption.AllDirectories)
            .Any(path => path.Contains("net10.0-browserwasm", StringComparison.OrdinalIgnoreCase)
                && path.Contains($"{Path.DirectorySeparatorChar}wwwroot{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase));
    }

    private static string? FindRepoRoot()
    {
        foreach (var start in new[] { Directory.GetCurrentDirectory(), AppContext.BaseDirectory })
        {
            var directory = new DirectoryInfo(start);
            while (directory is not null)
            {
                if (File.Exists(Path.Combine(directory.FullName, "src", "TaskFlow.slnx")))
                    return directory.FullName;

                directory = directory.Parent;
            }
        }

        return null;
    }

    internal sealed record AiStatus(string Provider, bool IsConfigured);
}

/// <summary>Describes the model provider wired into the Aspire test graph.</summary>
internal enum AspireAiProvider
{
    None,
    FoundryLocal,
    AzureFoundry
}
