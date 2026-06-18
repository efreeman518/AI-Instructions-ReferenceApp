using Microsoft.Playwright;
using Test.PlaywrightUI.PageObjects;

namespace Test.PlaywrightUI;

/// <summary>
/// Runs the stable Gateway and Blazor happy-path browser smoke.
/// </summary>
internal static class GatewayBlazorSmokeRunner
{
    /// <summary>
    /// Runs the smoke-test workflow against already-started Gateway and Blazor endpoints.
    /// </summary>
    public static async Task RunAsync(string gatewayBaseUrl, string blazorBaseUrl)
    {
        await EndpointProbe.EnsureReachableAsync($"{gatewayBaseUrl.TrimEnd('/')}/readyz", "Gateway");
        await EndpointProbe.EnsureReachableAsync($"{blazorBaseUrl.TrimEnd('/')}/readyz", "Blazor");

        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = true
        });

        var context = await browser.NewContextAsync(new BrowserNewContextOptions
        {
            IgnoreHTTPSErrors = true
        });
        var page = await context.NewPageAsync();

        var gateway = new GatewayPageObject(page);
        await gateway.AssertRootRespondsAsync(gatewayBaseUrl);
        await gateway.AssertAliveRespondsAsync(gatewayBaseUrl);

        var tasks = new BlazorTaskListPageObject(page);
        await tasks.NavigateAndAssertReadyAsync(blazorBaseUrl);
    }
}

/// <summary>
/// Probes HTTP endpoints before browser startup so failures point to hosting when the app is down.
/// </summary>
internal static class EndpointProbe
{
    internal static async Task EnsureReachableAsync(string url, string name)
    {
        using var handler = new HttpClientHandler { AllowAutoRedirect = false };
        using var http = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(30) };

        var deadline = DateTimeOffset.UtcNow.AddMinutes(2);
        while (DateTimeOffset.UtcNow < deadline)
        {
            try
            {
                using var response = await http.GetAsync(url);
                if ((int)response.StatusCode < 500)
                {
                    return;
                }
            }
            catch
            {
                // Aspire can bind the endpoint before the child app finishes booting.
            }

            await Task.Delay(TimeSpan.FromSeconds(2));
        }

        throw new InvalidOperationException($"{name} endpoint not reachable at {url}.");
    }
}
