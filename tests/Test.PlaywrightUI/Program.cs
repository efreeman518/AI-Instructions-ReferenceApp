using Microsoft.Playwright;
using Test.PlaywrightUI.Hosting;

namespace Test.PlaywrightUI;

internal static class Program
{
    public static async Task<int> Main()
    {
        try
        {
            await using var host = await PlaywrightAspireHost.StartAsync(CancellationToken.None);
            await GatewayBlazorSmokeRunner.RunAsync(host.GatewayBaseUrl, host.BlazorBaseUrl);
            Console.WriteLine("Gateway/Blazor Playwright smoke passed.");
            return 0;
        }
        catch (PlaywrightException ex) when (ex.Message.Contains("Executable doesn't exist", StringComparison.OrdinalIgnoreCase))
        {
            Console.Error.WriteLine("Playwright browser executable is missing.");
            Console.Error.WriteLine("Run: rtk powershell -NoProfile -File src\\Test\\Test.PlaywrightUI\\bin\\Debug\\net10.0\\playwright.ps1 install chromium");
            Console.Error.WriteLine(ex.Message);
            return 3;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Gateway/Blazor Playwright smoke failed: {ex.Message}");
            return 1;
        }
    }
}
