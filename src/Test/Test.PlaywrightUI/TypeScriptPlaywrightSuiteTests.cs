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
    [Timeout(900_000)]
    public async Task TypeScriptBrowserProjects_Pass()
    {
        if (!TypeScriptPlaywrightRunner.IsInstalled)
        {
            Assert.Inconclusive("TypeScript Playwright dependencies are missing. Run: npm install in src\\Test\\Test.PlaywrightUI.");
        }

        using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(12));
        await using var host = await PlaywrightAspireHost.StartAsync(cts.Token);

        var result = await TypeScriptPlaywrightRunner.RunAsync(host.TypeScriptProjects, cts.Token);
        TestContext.WriteLine(result.StandardOutput);
        TestContext.WriteLine(result.StandardError);

        Assert.AreEqual(
            0,
            result.ExitCode,
            $"TypeScript Playwright failed for project(s): {string.Join(", ", host.TypeScriptProjects)}.");
    }
}
