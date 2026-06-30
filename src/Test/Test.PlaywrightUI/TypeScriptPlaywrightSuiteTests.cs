using Test.PlaywrightUI.Hosting;

namespace Test.PlaywrightUI;

/// <summary>
/// MSTest adapter for the existing TypeScript Playwright browser projects.
/// </summary>
[TestClass]
[TestCategory("UI")]
[DoNotParallelize]
public sealed class TypeScriptPlaywrightSuiteTests
{
    /// <summary>
    /// Gets MSTest context for command output.
    /// </summary>
    public TestContext TestContext { get; set; } = null!;

    /// <summary>
    /// Runs the TypeScript Playwright projects for all browser hosts Aspire can expose locally.
    /// </summary>
    [TestMethod]
    [Timeout(3_600_000)]
    public async Task TypeScriptBrowserProjects_Pass()
    {
        var readiness = TypeScriptPlaywrightRunner.CheckReadiness();
        TestContext.WriteLine(readiness.Message);
        if (!readiness.CanRun)
        {
            Assert.Inconclusive(readiness.Message);
        }

        using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(55));
        PlaywrightAspireHost host;
        try
        {
            host = await PlaywrightAspireHost.StartAsync(cts.Token);
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

        await GatewayHttpSmokeRunner.RunAsync(host.GatewayBaseUrl, cts.Token);

        var result = await TypeScriptPlaywrightRunner.RunAsync(host.TypeScriptProjects, cts.Token);
        TestContext.WriteLine(result.StandardOutput);
        TestContext.WriteLine(result.StandardError);

        if (result.ExitCode != 0)
        {
            Assert.Fail(
                $"TypeScript Playwright failed with exit code {result.ExitCode} for project(s): {string.Join(", ", host.TypeScriptProjects)}."
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
