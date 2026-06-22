using Aspire.Hosting;
using Aspire.Hosting.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Test.PlaywrightUI.Hosting;

/// <summary>
/// Starts the AppHost test mesh and exposes browser endpoints for C# and TypeScript Playwright tests.
/// </summary>
internal sealed class PlaywrightAspireHost : IAsyncDisposable
{
    internal const string DefaultGatewayUrl = "http://localhost:5007";
    internal const string DefaultBlazorUrl = "https://localhost:7201";

    private const string GatewayResourceName = "taskflowgateway";
    private const string BlazorResourceName = "taskflowblazor";
    private const string ReactResourceName = "taskflowreact";
    private const string HttpEndpointName = "http";
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromMinutes(8);
    private static readonly TimeSpan CleanupTimeout = TimeSpan.FromMinutes(1);

    private readonly DistributedApplication _app;
    private readonly Dictionary<string, string?> _originalEnvironment;

    private PlaywrightAspireHost(
        DistributedApplication app,
        string gatewayBaseUrl,
        string blazorBaseUrl,
        IReadOnlyList<string> typeScriptProjects,
        Dictionary<string, string?> originalEnvironment)
    {
        _app = app;
        GatewayBaseUrl = gatewayBaseUrl.TrimEnd('/');
        BlazorBaseUrl = blazorBaseUrl.TrimEnd('/');
        TypeScriptProjects = typeScriptProjects;
        _originalEnvironment = originalEnvironment;
    }

    internal string GatewayBaseUrl { get; }

    internal string BlazorBaseUrl { get; }

    internal IReadOnlyList<string> TypeScriptProjects { get; }

    internal static async Task<PlaywrightAspireHost> StartAsync(CancellationToken ct)
    {
        var originalEnvironment = CaptureEnvironment(
            "TASKFLOW_ASPIRE_TESTING",
            "TASKFLOW_ASPIRE_REACT_AVAILABLE",
            "PLAYWRIGHT_GATEWAY_URL",
            "PLAYWRIGHT_BLAZOR_URL",
            "PLAYWRIGHT_REACT_URL",
            "PLAYWRIGHT_UNO_URL");

        var reactRunnable = IsReactRunnable();

        Environment.SetEnvironmentVariable("TASKFLOW_ASPIRE_TESTING", "true");
        SetOrClear("TASKFLOW_ASPIRE_REACT_AVAILABLE", reactRunnable);

        try
        {
            var appHostProgramType = Type.GetType("Program, AppHost", throwOnError: true)!;
            var builder = await DistributedApplicationTestingBuilder.CreateAsync(
                appHostProgramType,
                args: [],
                configureBuilder: (appOptions, _) =>
                {
                    appOptions.DisableDashboard = true;
                    appOptions.EnableResourceLogging = false;
                },
                cancellationToken: ct).WaitAsync(DefaultTimeout, ct);

            builder.Services.AddLogging(logging =>
            {
                logging.SetMinimumLevel(LogLevel.Information);
                logging.AddFilter("Microsoft.AspNetCore", LogLevel.Warning);
                logging.AddFilter("Aspire.", LogLevel.Warning);
            });

            var app = await builder.BuildAsync(ct).WaitAsync(DefaultTimeout, ct);
            await app.StartAsync(ct).WaitAsync(DefaultTimeout, ct);

            var gatewayBaseUrl = await ResolveEndpointAsync(
                app,
                GatewayResourceName,
                "PLAYWRIGHT_GATEWAY_URL",
                ct);

            var blazorBaseUrl = await ResolveEndpointAsync(
                app,
                BlazorResourceName,
                "PLAYWRIGHT_BLAZOR_URL",
                ct);

            var typeScriptProjects = new List<string> { "blazor" };

            if (HasValue("PLAYWRIGHT_REACT_URL") || HasValue("TASKFLOW_REACT_BASE_URL"))
            {
                var reactBaseUrl = Environment.GetEnvironmentVariable("PLAYWRIGHT_REACT_URL")
                    ?? Environment.GetEnvironmentVariable("TASKFLOW_REACT_BASE_URL");
                Environment.SetEnvironmentVariable("PLAYWRIGHT_REACT_URL", reactBaseUrl?.TrimEnd('/'));
                typeScriptProjects.Add("react");
            }
            else if (reactRunnable)
            {
                await ResolveEndpointAsync(app, ReactResourceName, "PLAYWRIGHT_REACT_URL", ct);
                typeScriptProjects.Add("react");
            }

            if (HasValue("PLAYWRIGHT_UNO_URL"))
            {
                typeScriptProjects.Add("uno");
            }

            return new PlaywrightAspireHost(
                app,
                gatewayBaseUrl,
                blazorBaseUrl,
                typeScriptProjects,
                originalEnvironment);
        }
        catch
        {
            RestoreEnvironment(originalEnvironment);
            throw;
        }
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            await _app.StopAsync().WaitAsync(CleanupTimeout);
        }
        catch (TimeoutException)
        {
            // Bounded cleanup: DisposeAsync still releases Aspire-owned resources.
        }
        catch
        {
            // Test process is already failing; still dispose and restore environment below.
        }
        finally
        {
            await _app.DisposeAsync();
            RestoreEnvironment(_originalEnvironment);
        }
    }

    private static async Task<string> ResolveEndpointAsync(
        DistributedApplication app,
        string resourceName,
        string environmentVariableName,
        CancellationToken ct)
    {
        var configured = Environment.GetEnvironmentVariable(environmentVariableName);
        if (!string.IsNullOrWhiteSpace(configured))
        {
            return configured.TrimEnd('/');
        }

        await app.ResourceNotifications.WaitForResourceHealthyAsync(resourceName, ct)
            .WaitAsync(DefaultTimeout, ct);

        var endpoint = app.GetEndpoint(resourceName, HttpEndpointName).ToString().TrimEnd('/');
        Environment.SetEnvironmentVariable(environmentVariableName, endpoint);
        return endpoint;
    }

    private static Dictionary<string, string?> CaptureEnvironment(params string[] names) =>
        names.ToDictionary(name => name, Environment.GetEnvironmentVariable);

    private static void RestoreEnvironment(IReadOnlyDictionary<string, string?> values)
    {
        foreach (var (name, value) in values)
        {
            Environment.SetEnvironmentVariable(name, value);
        }
    }

    private static void SetOrClear(string name, bool enabled) =>
        Environment.SetEnvironmentVariable(name, enabled ? "true" : null);

    private static bool HasValue(string variableName) =>
        !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(variableName));

    private static bool IsReactRunnable()
    {
        var reactRoot = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..", "..",
            "UI",
            "TaskFlow.React"));
        var viteShim = OperatingSystem.IsWindows()
            ? Path.Combine(reactRoot, "node_modules", ".bin", "vite.cmd")
            : Path.Combine(reactRoot, "node_modules", ".bin", "vite");

        return File.Exists(Path.Combine(reactRoot, "package.json"))
            && File.Exists(viteShim)
            && CommandSucceeds(OperatingSystem.IsWindows() ? "node.exe" : "node", "--version");
    }

    private static bool CommandSucceeds(string fileName, string arguments)
    {
        try
        {
            using var process = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(fileName, arguments)
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
}
