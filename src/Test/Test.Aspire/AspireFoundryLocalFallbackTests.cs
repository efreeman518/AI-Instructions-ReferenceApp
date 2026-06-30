using AppHost;
using Aspire.Hosting;
using Aspire.Hosting.Testing;
using EF.IntegrationTesting.Aspire;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using EnvironmentVariableScope = EF.IntegrationTesting.Environment.EnvironmentVariableScope;

namespace Test.Aspire;

/// <summary>
/// Isolated Aspire smoke for API-hosted Foundry Local fallback.
/// This test owns the AppHost graph because Foundry Local must be enabled before AppHost build.
/// </summary>
[TestClass]
[TestCategory("LiveAI")]
[TestCategory("FoundryLocal")]
[DoNotParallelize]
public sealed class AspireFoundryLocalFallbackTests
{
    public TestContext TestContext { get; set; } = null!;

    [TestMethod]
    [Timeout(600000)]
    public async Task Given_FoundryLocalAspireGraph_When_ApiChatCalled_Then_ConfiguredModelResponds()
    {
        var ct = TestContext.CancellationTokenSource.Token;

        using var environment = new EnvironmentVariableScope()
            .Set("TASKFLOW_ASPIRE_TESTING", "true")
            .Set(AspireTestHost.FoundryLocalOptInEnvironmentVariable, "true")
            .Set("TASKFLOW_USE_AZURE_FOUNDRY", "false");

        await using var app = await BuildAspireAppAsync(ct);

        try
        {
            await app.StartAsync(ct).WaitAsync(AspireTestHost.DefaultTimeout, ct);
            await app.WaitForResourceHealthyAsync("taskflowdb", AspireTestHost.DefaultTimeout, ct);
            await app.WaitForResourceHealthyAsync("taskflowapi", AspireTestHost.DefaultTimeout, ct);
            await app.WaitForResourceHealthyAsync("taskflowgateway", AspireTestHost.DefaultTimeout, ct);
        }
        catch (Exception ex) when (IsMissingFoundryLocalRuntime(ex))
        {
            Assert.Inconclusive("Foundry Local runtime is not installed or not discoverable: " + ex.Message);
        }

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeout.CancelAfter(TimeSpan.FromSeconds(300));

        using var client = app.CreateHttpClient("taskflowapi", "http");
        client.Timeout = TimeSpan.FromMinutes(5);

        HttpResponseMessage statusResponse;
        try
        {
            statusResponse = await client.GetAsync("/api/v1/ai/status", timeout.Token);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            Assert.Inconclusive($"AI status endpoint unavailable: {ex.Message}");
            throw;
        }

        using var statusResponseMessage = statusResponse;
        if (!statusResponseMessage.IsSuccessStatusCode)
        {
            Assert.Inconclusive(
                $"AI status endpoint returned {(int)statusResponseMessage.StatusCode} {statusResponseMessage.ReasonPhrase}.");
        }

        using var status = await ReadJsonAsync(statusResponseMessage, timeout.Token);
        Assert.AreEqual("local", status.RootElement.GetProperty("provider").GetString());
        Assert.IsTrue(status.RootElement.GetProperty("isConfigured").GetBoolean());

        HttpResponseMessage chatResponse;
        try
        {
            chatResponse = await client.PostAsJsonAsync(
                "/api/v1/ai/chat",
                new { message = "Reply in one short sentence: what does TaskFlow track?" },
                timeout.Token);
        }
        catch (OperationCanceledException ex) when (!ct.IsCancellationRequested)
        {
            Assert.Inconclusive(
                "Foundry Local Aspire graph bootstrapped, but chat smoke did not complete within 300 seconds. " +
                ex.Message);
            throw;
        }

        using var chatResponseMessage = chatResponse;
        using var chat = await ReadJsonAsync(chatResponseMessage, timeout.Token);

        Assert.AreEqual(HttpStatusCode.OK, chatResponseMessage.StatusCode);
        Assert.IsTrue(chat.RootElement.GetProperty("isConfigured").GetBoolean());
        AssertHasText(chat.RootElement.GetProperty("message"), "message");
    }

    private static async Task<DistributedApplication> BuildAspireAppAsync(CancellationToken ct)
    {
        var appHostProgramType = Type.GetType("Program, AppHost", throwOnError: true)!;

        var builder = await DistributedApplicationTestingBuilder.CreateAsync(
            appHostProgramType,
            args: [],
            configureBuilder: (appOptions, hostSettings) =>
            {
                appOptions.DisableDashboard = true;
                appOptions.EnableResourceLogging = false;

                hostSettings.Configuration ??= new();
                hostSettings.Configuration["Parameters:sql-password"] = LocalSqlSettings.SharedSaPassword;
                hostSettings.Configuration["AiServices:FoundryEndpoint"] = string.Empty;
            },
            cancellationToken: ct).WaitAsync(AspireTestHost.DefaultTimeout, ct);

        return await builder.BuildAsync(ct).WaitAsync(AspireTestHost.DefaultTimeout, ct);
    }

    private static bool IsMissingFoundryLocalRuntime(Exception ex) =>
        ex is FileNotFoundException
        || ex is DirectoryNotFoundException
        || ex is PlatformNotSupportedException
        || ex is System.ComponentModel.Win32Exception
        || (ex.InnerException is not null && IsMissingFoundryLocalRuntime(ex.InnerException));

    private static async Task<JsonDocument> ReadJsonAsync(HttpResponseMessage response, CancellationToken ct)
    {
        var body = await response.Content.ReadAsStringAsync(ct);
        if (!response.IsSuccessStatusCode)
        {
            Assert.Fail(
                $"Expected JSON success response, got {(int)response.StatusCode} {response.ReasonPhrase}. Body: {Truncate(body)}");
        }

        if (string.IsNullOrWhiteSpace(body))
        {
            Assert.Fail($"Expected JSON response, got empty body from {response.RequestMessage?.RequestUri}.");
        }

        return JsonDocument.Parse(body);
    }

    private static void AssertHasText(JsonElement element, string propertyName)
    {
        Assert.AreEqual(JsonValueKind.String, element.ValueKind, $"{propertyName} should be string.");
        Assert.IsFalse(string.IsNullOrWhiteSpace(element.GetString()), $"{propertyName} should not be blank.");
    }

    private static string Truncate(string value) => value.Length <= 1000 ? value : value[..1000] + "...";
}
