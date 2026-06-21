using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Aspire.Hosting.Testing;

namespace Test.Aspire;

/// <summary>
/// Smoke tests for model-backed AI endpoints. Azure runs through the Aspire-hosted gateway; Foundry
/// Local calls the API directly because cold local model responses can exceed gateway timeouts.
///
/// Selection:
/// 1. Azure Foundry runs when explicit Azure config is present.
/// 2. Foundry Local runs when explicitly requested, or by default when Azure config is absent.
/// 3. With no model provider, these tests are inconclusive instead of silently passing.
///
/// <c>TASKFLOW_LIVE_AI_BASE_URL</c> can override the request target for manual runs, but it is not an opt-in.
/// </summary>
[TestClass]
[TestCategory("LiveAI")]
[TestCategory("Foundry")]
[DoNotParallelize]
public class AiFoundryLiveSmokeTests
{
    private const string LiveBaseUrlVariable = "TASKFLOW_LIVE_AI_BASE_URL";
    private const string ApiPrefix = "api/v1/";

    /// <summary>Gets MSTest context for cancellation and diagnostic output.</summary>
    public TestContext TestContext { get; set; } = null!;

    /// <summary>Boots the shared Aspire graph before model-backed endpoint checks run.</summary>
    [ClassInitialize]
    public static Task ClassInit(TestContext context) => AspireTestHost.EnsureStartedAsync(context);

    /// <summary>Verifies D1 reaches the active configured model and returns non-empty assistant text.</summary>
    [TestMethod]
    [Timeout(360000)]
    public async Task Given_FoundryBackedAppHost_When_ChatEndpointCalled_Then_ConfiguredModelResponds()
    {
        using var client = await CreateFoundryClientOrInconclusiveAsync();

        await AssertConfiguredChatAsync(client, "Answer in one short sentence: what does TaskFlow track?");
    }

    /// <summary>Verifies D3 reaches the code-hosted task assistant agent over the active model.</summary>
    [TestMethod]
    [Timeout(360000)]
    public async Task Given_FoundryBackedAppHost_When_AgentChatEndpointCalled_Then_ConfiguredAgentResponds()
    {
        using var client = await CreateFoundryClientOrInconclusiveAsync();

        var ct = TestContext.CancellationTokenSource.Token;
        using var response = await client.PostAsJsonAsync(
            ApiPath(client, "api/v1/agent/chat"),
            new
            {
                message = "Answer in one short sentence: what can the TaskFlow assistant help with?",
                conversationId = Guid.NewGuid().ToString("N")
            },
            ct);
        using var payload = await ReadJsonAsync(response, ct);

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        Assert.IsTrue(payload.RootElement.GetProperty("isConfigured").GetBoolean());
        AssertHasText(payload.RootElement.GetProperty("message"), "message");
        AssertHasText(payload.RootElement.GetProperty("conversationId"), "conversationId");
    }

    /// <summary>Verifies D4 can classify a real task through the active model without applying writes.</summary>
    [TestMethod]
    [Timeout(360000)]
    public async Task Given_FoundryBackedAppHost_When_TaskTriageCalled_Then_TriageContractReturnedWithoutApplyingWrites()
    {
        using var client = await CreateFoundryClientOrInconclusiveAsync();

        var ct = TestContext.CancellationTokenSource.Token;
        var taskId = await CreateTaskAsync(client, "Live AI triage smoke " + Guid.NewGuid().ToString("N"), ct);
        using var response = await client.PostAsync(ApiPath(client, $"api/v1/ai/triage/{taskId}?apply=false"), null, ct);
        using var payload = await ReadJsonAsync(response, ct);

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        Assert.IsTrue(payload.RootElement.GetProperty("isConfigured").GetBoolean());
        Assert.IsFalse(payload.RootElement.GetProperty("applied").GetBoolean());
        var triage = payload.RootElement.GetProperty("triage");
        if (triage.ValueKind == JsonValueKind.Null)
        {
            AssertHasText(payload.RootElement.GetProperty("error"), "error");
            return;
        }

        Assert.AreEqual(JsonValueKind.Object, triage.ValueKind);
        AssertHasText(triage.GetProperty("suggestedPriority"), "suggestedPriority");
    }

    private async Task AssertConfiguredChatAsync(HttpClient client, string message)
    {
        var ct = TestContext.CancellationTokenSource.Token;
        using var response = await client.PostAsJsonAsync(ApiPath(client, "api/v1/ai/chat"), new { message }, ct);
        using var payload = await ReadJsonAsync(response, ct);

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        Assert.IsTrue(payload.RootElement.GetProperty("isConfigured").GetBoolean());
        var assistantMessage = payload.RootElement.GetProperty("message").GetString();
        Assert.IsFalse(string.IsNullOrWhiteSpace(assistantMessage));
        StringAssert.DoesNotMatch(assistantMessage, new("not configured", System.Text.RegularExpressions.RegexOptions.IgnoreCase));
    }

    internal async Task<HttpClient> CreateFoundryClientOrInconclusiveAsync()
    {
        if (AspireTestHost.AiProvider == AspireAiProvider.None)
        {
            Assert.Inconclusive(
                "No Foundry provider available. Configure Azure AI Foundry or enable Foundry Local to run live Foundry smoke tests.");
            throw new InvalidOperationException("Unreachable");
        }

        var baseUrl = Environment.GetEnvironmentVariable(LiveBaseUrlVariable);
        HttpClient client;
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            client = AspireTestHost.AiProvider == AspireAiProvider.AzureFoundry
                ? await CreateAspireGatewayClientAsync()
                : await CreateAspireApiClientAsync();
        }
        else
        {
            var baseUri = new Uri(baseUrl.TrimEnd('/') + "/", UriKind.Absolute);
            var handler = new HttpClientHandler();

            // Aspire dev HTTPS commonly uses a local development certificate. Trust only loopback here.
            if (baseUri.IsLoopback)
                handler.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;

            client = new HttpClient(handler)
            {
                BaseAddress = baseUri,
                Timeout = TimeSpan.FromMinutes(3)
            };
        }

        try
        {
            await AssertFoundryConfiguredOrInconclusiveAsync(client);
            return client;
        }
        catch
        {
            client.Dispose();
            throw;
        }
    }

    private async Task<HttpClient> CreateAspireGatewayClientAsync()
    {
        var ct = TestContext.CancellationTokenSource.Token;
        await AspireTestHost.WaitForResourceHealthyAsync("taskflowapi", ct);
        await AspireTestHost.WaitForResourceHealthyAsync("taskflowgateway", ct);

        var client = AspireTestHost.AspireApp!.CreateHttpClient("taskflowgateway", "http");
        client.Timeout = TimeSpan.FromMinutes(3);
        return client;
    }

    private async Task<HttpClient> CreateAspireApiClientAsync()
    {
        var ct = TestContext.CancellationTokenSource.Token;
        await AspireTestHost.WaitForResourceHealthyAsync("taskflowapi", ct);

        var client = AspireTestHost.AspireApp!.CreateHttpClient("taskflowapi", "http");
        client.Timeout = TimeSpan.FromMinutes(5);
        return client;
    }

    private static string ApiPath(HttpClient client, string apiPath)
    {
        var normalized = apiPath.TrimStart('/');
        var basePath = client.BaseAddress?.AbsolutePath.Trim('/');

        return string.Equals(basePath, "api/v1", StringComparison.OrdinalIgnoreCase)
            && normalized.StartsWith(ApiPrefix, StringComparison.OrdinalIgnoreCase)
                ? normalized[ApiPrefix.Length..]
                : normalized;
    }

    private async Task<Guid> CreateTaskAsync(HttpClient client, string title, CancellationToken ct)
    {
        using var response = await client.PostAsJsonAsync(
            ApiPath(client, "api/v1/task-items"),
            new { item = new { title, priority = 2 } },
            ct);
        using var payload = await ReadJsonAsync(response, ct);

        Assert.AreEqual(HttpStatusCode.Created, response.StatusCode);
        var id = payload.RootElement.GetProperty("item").GetProperty("id").GetGuid();
        Assert.AreNotEqual(Guid.Empty, id);
        return id;
    }

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

        try
        {
            return JsonDocument.Parse(body);
        }
        catch (JsonException ex)
        {
            Assert.Fail($"Expected JSON response. Parse error: {ex.Message}. Body: {Truncate(body)}");
            throw;
        }
    }

    private static string Truncate(string value) =>
        value.Length <= 1000 ? value : value[..1000] + "...";

    private async Task AssertFoundryConfiguredOrInconclusiveAsync(HttpClient client)
    {
        var ct = TestContext.CancellationTokenSource.Token;
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeout.CancelAfter(TimeSpan.FromSeconds(15));

        try
        {
            using var response = await client.GetAsync(ApiPath(client, "api/v1/ai/status"), timeout.Token);
            if (!response.IsSuccessStatusCode)
            {
                Assert.Inconclusive($"AI status endpoint returned {(int)response.StatusCode} {response.ReasonPhrase}.");
            }

            var status = await response.Content.ReadFromJsonAsync<AspireTestHost.AiStatus>(cancellationToken: timeout.Token);
            if (status?.IsConfigured != true)
            {
                Assert.Inconclusive(
                    "No Foundry provider available. Azure Foundry is not configured and Foundry Local did not bootstrap, so API is using no-op AI.");
            }
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            Assert.Inconclusive($"AI status endpoint unavailable: {ex.Message}");
        }
    }

    private static void AssertHasText(JsonElement element, string propertyName)
    {
        Assert.AreEqual(JsonValueKind.String, element.ValueKind, $"{propertyName} should be a JSON string.");
        Assert.IsFalse(string.IsNullOrWhiteSpace(element.GetString()), $"{propertyName} should not be blank.");
    }
}
