using Microsoft.Playwright;
using Test.PlaywrightUI.Hosting;

namespace Test.PlaywrightUI;

/// <summary>
/// Browser smoke coverage for the stable Gateway-to-Blazor happy path.
/// </summary>
[TestClass]
[TestCategory("UI")]
[DoNotParallelize]
public sealed class GatewayBlazorSmokeTests
{
    /// <summary>
    /// Starts Aspire and verifies the Gateway root plus Blazor task-list surface.
    /// </summary>
    [TestMethod]
    [Timeout(600_000)]
    public async Task GatewayAndBlazorHappyPath_Passes()
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(8));
        await using var host = await PlaywrightAspireHost.StartAsync(cts.Token);

        try
        {
            await GatewayBlazorSmokeRunner.RunAsync(host.GatewayBaseUrl, host.BlazorBaseUrl);
        }
        catch (PlaywrightException ex) when (ex.Message.Contains("Executable doesn't exist", StringComparison.OrdinalIgnoreCase))
        {
            Assert.Fail(
                "Playwright browser executable is missing. Run: " +
                "rtk powershell -NoProfile -File src\\Test\\Test.PlaywrightUI\\bin\\Debug\\net10.0\\playwright.ps1 install chromium");
        }
    }
}
