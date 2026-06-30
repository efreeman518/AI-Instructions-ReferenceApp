using Test.PlaywrightUI.Hosting;

namespace Test.PlaywrightUI;

/// <summary>
/// MSTest adapter for existing TypeScript Playwright browser projects.
/// </summary>
[TestClass]
[DoNotParallelize]
public sealed class TypeScriptPlaywrightSuiteTests
{
    /// <summary>
    /// Gets MSTest context command output.
    /// </summary>
    public TestContext TestContext { get; set; } = null!;

    /// <summary>
    /// Runs the Blazor TypeScript Playwright project through the Aspire-owned host.
    /// </summary>
    [TestMethod]
    [TestCategory("PlaywrightUI")]
    [Timeout(3_600_000)]
    public Task BlazorTypeScriptProject_Passes() => RunTypeScriptProjectsAsync("blazor");

    /// <summary>
    /// Runs the React TypeScript Playwright project when that host is available.
    /// </summary>
    [TestMethod]
    [TestCategory("PlaywrightUI")]
    [Timeout(3_600_000)]
    public Task ReactTypeScriptProject_Passes() => RunTypeScriptProjectsAsync("react");

    /// <summary>
    /// Runs the Uno WASM TypeScript Playwright canvas smoke project.
    /// </summary>
    [TestMethod]
    [TestCategory("WasmUI")]
    [Timeout(3_600_000)]
    public Task UnoWasmCanvasSmoke_Passes() => RunTypeScriptProjectsAsync("uno");

    private async Task RunTypeScriptProjectsAsync(params string[] requestedProjects)
    {
        var readiness = TypeScriptPlaywrightRunner.CheckReadiness();
        TestContext.WriteLine(readiness.Message);
        if (!readiness.CanRun)
        {
            Assert.Inconclusive(readiness.Message);
            return;
        }

        using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(55));
        PlaywrightAspireHost host;
        try
        {
            host = await PlaywrightAspireHost.StartAsync(requestedProjects, cts.Token);
        }
        catch (WasmPrerequisiteException ex)
        {
            Assert.Inconclusive(ex.Message);
            return;
        }

        await using var hostScope = host;
        foreach (var message in host.DiagnosticMessages)
        {
            TestContext.WriteLine(message);
        }

        var availableProjects = host.TypeScriptProjects.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var selectedProjects = requestedProjects.Where(availableProjects.Contains).ToArray();
        if (selectedProjects.Length == 0)
        {
            Assert.Inconclusive(
                $"Requested TypeScript Playwright project(s) unavailable: {string.Join(", ", requestedProjects)}. "
                + $"Available: {string.Join(", ", host.TypeScriptProjects)}.");
            return;
        }

        await GatewayHttpSmokeRunner.RunAsync(host.GatewayBaseUrl, cts.Token);

        var result = await TypeScriptPlaywrightRunner.RunAsync(selectedProjects, cts.Token);
        TestContext.WriteLine(result.StandardOutput);
        TestContext.WriteLine(result.StandardError);

        if (result.ExitCode != 0)
        {
            Assert.Fail(
                $"TypeScript Playwright failed with exit code {result.ExitCode} project(s): {string.Join(", ", selectedProjects)}."
                + Environment.NewLine
                + "stdout:"
                + Environment.NewLine
                + Truncate(result.StandardOutput)
                + Environment.NewLine
                + "stderr:"
                + Environment.NewLine
                + Truncate(result.StandardError));
        }
    }

    private static string Truncate(string value)
        => value.Length <= 12_000 ? value : value[..12_000] + "...";
}
