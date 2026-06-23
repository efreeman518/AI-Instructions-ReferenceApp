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
    [Timeout(1_200_000)]
    public async Task TypeScriptBrowserProjects_Pass()
    {
        var readiness = TypeScriptPlaywrightRunner.CheckReadiness();
        TestContext.WriteLine(readiness.Message);
        if (!readiness.CanRun)
        {
            Assert.Inconclusive(readiness.Message);
        }

        using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(18));
        await using var host = await PlaywrightAspireHost.StartAsync(cts.Token);

        await GatewayHttpSmokeRunner.RunAsync(host.GatewayBaseUrl, cts.Token);

        var result = await TypeScriptPlaywrightRunner.RunAsync(host.TypeScriptProjects, cts.Token);
        TestContext.WriteLine(result.StandardOutput);
        TestContext.WriteLine(result.StandardError);

        Assert.AreEqual(
            0,
            result.ExitCode,
            $"TypeScript Playwright failed for project(s): {string.Join(", ", host.TypeScriptProjects)}.");
    }
}
