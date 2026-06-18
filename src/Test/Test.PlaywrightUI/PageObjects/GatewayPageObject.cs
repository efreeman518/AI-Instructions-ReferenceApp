using Microsoft.Playwright;

namespace Test.PlaywrightUI.PageObjects;

/// <summary>
/// Wraps the stable Gateway browser-facing checks used by Playwright smoke tests.
/// </summary>
public sealed class GatewayPageObject(IPage page)
{
    private const int NavigationTimeout = 60_000;

    /// <summary>
    /// Navigates to the Gateway root and verifies the public marker response.
    /// </summary>
    public async Task AssertRootRespondsAsync(string baseUrl)
    {
        await page.GotoAsync(
            baseUrl.TrimEnd('/'),
            new PageGotoOptions
            {
                WaitUntil = WaitUntilState.DOMContentLoaded,
                Timeout = NavigationTimeout
            });

        var bodyText = await page.Locator("body").InnerTextAsync(new LocatorInnerTextOptions
        {
            Timeout = 10_000
        });

        if (!bodyText.Contains("TaskFlow Gateway", StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"Expected Gateway marker text, got '{bodyText}'.");
        }
    }

    /// <summary>
    /// Navigates to the lightweight Gateway liveness endpoint and verifies its marker response.
    /// </summary>
    public async Task AssertAliveRespondsAsync(string baseUrl)
    {
        await page.GotoAsync(
            $"{baseUrl.TrimEnd('/')}/alive",
            new PageGotoOptions
            {
                WaitUntil = WaitUntilState.DOMContentLoaded,
                Timeout = NavigationTimeout
            });

        var bodyText = await page.Locator("body").InnerTextAsync(new LocatorInnerTextOptions
        {
            Timeout = 10_000
        });

        if (!bodyText.Contains("Alive", StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"Expected Gateway alive marker, got '{bodyText}'.");
        }
    }
}
