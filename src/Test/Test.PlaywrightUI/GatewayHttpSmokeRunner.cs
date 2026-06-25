namespace Test.PlaywrightUI;

/// <summary>
/// Lightweight gateway smoke checks that do not require a second browser session.
/// </summary>
internal static class GatewayHttpSmokeRunner
{
    internal static async Task RunAsync(string gatewayBaseUrl, CancellationToken ct)
    {
        using var handler = new HttpClientHandler { AllowAutoRedirect = false };
        using var http = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(30) };

        await AssertContainsAsync(http, gatewayBaseUrl.TrimEnd('/'), "TaskFlow Gateway", ct);
        await AssertContainsAsync(http, $"{gatewayBaseUrl.TrimEnd('/')}/alive", "Alive", ct);
    }

    private static async Task AssertContainsAsync(HttpClient http, string url, string expected, CancellationToken ct)
    {
        using var response = await http.GetAsync(url, ct);
        var body = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode || !body.Contains(expected, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"Expected {url} to return success and contain '{expected}', got {(int)response.StatusCode}: {body}");
        }
    }
}
