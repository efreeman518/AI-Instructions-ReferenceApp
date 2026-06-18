using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Aspire.Hosting.Testing;

namespace Test.Aspire;

/// <summary>
/// Smoke tests for model-backed AI endpoints running through the Aspire-hosted gateway by default.
/// They assert response contracts instead of exact model text.
///
/// Selection:
/// 1. Azure Foundry runs when explicit Azure config is present.
/// 2. Foundry Local runs when Azure config is absent and the local foundry CLI/service/model probe succeeds.
/// 3. With no model provider, these tests are inconclusive and the no-model AI tests assert the fallback path.
///
/// <c>TASKFLOW_LIVE_AI_BASE_URL</c> can override the request target for manual runs, but it is not an opt-in.
/// </summary>
[TestClass]
[TestCategory("LiveAI")]
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

    /// <summary>Verifies D1 reaches a configured local model and returns non-empty assistant text.</summary>
    [TestMethod]
    [TestCategory("FoundryLocal")]
    [Timeout(180000)]
    public async Task Given_FoundryLocalAppHost_When_ChatEndpointCalled_Then_ConfiguredModelResponds()
    {
        using var client = await CreateClientOrSkipAsync(AspireAiProvider.FoundryLocal, "Foundry Local");

        await AssertConfiguredChatAsync(client, "Answer in one short sentence: what does TaskFlow track?");
    }

    /// <summary>Verifies D2 streams tokens from the configured local model.</summary>
    [TestMethod]
    [TestCategory("FoundryLocal")]
    [Timeout(180000)]
    public async Task Given_FoundryLocalAppHost_When_StreamingChatEndpointCalled_Then_TokensReturned()
    {
        using var client = await CreateClientOrSkipAsync(AspireAiProvider.FoundryLocal, "Foundry Local");

        var ct = TestContext.CancellationTokenSource.Token;
        using var response = await client.PostAsJsonAsync(
            ApiPath(client, "api/v1/ai/chat/stream"),
            new { message = "Answer with exactly one short sentence about task planning." },
            ct);
        var body = await response.Content.ReadAsStringAsync(ct);

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        Assert.IsFalse(string.IsNullOrWhiteSpace(body));
        StringAssert.Contains(body, "data:");
    }

    /// <summary>Verifies D3 reaches the code-hosted task assistant agent over the local model.</summary>
    [TestMethod]
    [TestCategory("FoundryLocal")]
    [Timeout(180000)]
    public async Task Given_FoundryLocalAppHost_When_AgentChatEndpointCalled_Then_ConfiguredAgentResponds()
    {
        using var client = await CreateClientOrSkipAsync(AspireAiProvider.FoundryLocal, "Foundry Local");

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

    /// <summary>Verifies D4 can classify a real task through the local model without applying writes.</summary>
    [TestMethod]
    [TestCategory("FoundryLocal")]
    [Timeout(180000)]
    public async Task Given_FoundryLocalAppHost_When_TaskTriageCalled_Then_TriageContractReturned()
    {
        using var client = await CreateClientOrSkipAsync(AspireAiProvider.FoundryLocal, "Foundry Local");

        var ct = TestContext.CancellationTokenSource.Token;
        var taskId = await CreateTaskAsync(client, "Live AI triage smoke " + Guid.NewGuid().ToString("N"), ct);
        using var response = await client.PostAsync(ApiPath(client, $"api/v1/ai/triage/{taskId}?apply=false"), null, ct);
        using var payload = await ReadJsonAsync(response, ct);

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        Assert.IsTrue(payload.RootElement.GetProperty("isConfigured").GetBoolean());
        Assert.IsFalse(payload.RootElement.GetProperty("applied").GetBoolean());
        var triage = payload.RootElement.GetProperty("triage");
        Assert.AreEqual(JsonValueKind.Object, triage.ValueKind);
        AssertHasText(triage.GetProperty("suggestedPriority"), "suggestedPriority");
    }

    /// <summary>
    /// Verifies D5 reaches the model-backed draft path. Small local models may return non-parseable JSON,
    /// so this smoke accepts either a created task or the app's parse-guard error as long as AI was configured.
    /// </summary>
    [TestMethod]
    [TestCategory("FoundryLocal")]
    [Timeout(180000)]
    public async Task Given_FoundryLocalAppHost_When_DraftTaskCalled_Then_ModelBackedPathIsStable()
    {
        using var client = await CreateClientOrSkipAsync(AspireAiProvider.FoundryLocal, "Foundry Local");

        var ct = TestContext.CancellationTokenSource.Token;
        using var response = await client.PostAsJsonAsync(
            ApiPath(client, "api/v1/ai/tasks/draft"),
            new { title = "Prepare live AI smoke test notes" },
            ct);
        using var payload = await ReadJsonAsync(response, ct);

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        Assert.IsTrue(payload.RootElement.GetProperty("isConfigured").GetBoolean());

        if (payload.RootElement.GetProperty("created").GetBoolean())
        {
            Assert.AreNotEqual(JsonValueKind.Null, payload.RootElement.GetProperty("taskId").ValueKind);
            AssertHasText(payload.RootElement.GetProperty("description"), "description");
            return;
        }

        AssertHasText(payload.RootElement.GetProperty("error"), "error");
    }

    /// <summary>Verifies D7 read-only recommendation works against a configured local model.</summary>
    [TestMethod]
    [TestCategory("FoundryLocal")]
    [Timeout(180000)]
    public async Task Given_FoundryLocalAppHost_When_NextActionCalled_Then_RecommendationReturned()
    {
        using var client = await CreateClientOrSkipAsync(AspireAiProvider.FoundryLocal, "Foundry Local");

        var ct = TestContext.CancellationTokenSource.Token;
        using var response = await client.PostAsync(ApiPath(client, "api/v1/ai/next-action"), null, ct);
        using var payload = await ReadJsonAsync(response, ct);

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        Assert.IsTrue(payload.RootElement.GetProperty("isConfigured").GetBoolean());
        AssertHasText(payload.RootElement.GetProperty("recommendation"), "recommendation");
    }

    /// <summary>
    /// Verifies D1 against Azure Foundry when the Aspire test graph detects an Azure-backed chat connection.
    /// </summary>
    [TestMethod]
    [TestCategory("AzureFoundry")]
    [Timeout(180000)]
    public async Task Given_AzureFoundryAppHost_When_ChatEndpointCalled_Then_ConfiguredModelResponds()
    {
        using var client = await CreateClientOrSkipAsync(AspireAiProvider.AzureFoundry, "Azure Foundry");

        await AssertConfiguredChatAsync(client, "Answer in one short sentence: what does TaskFlow track?");
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

    private async Task<HttpClient> CreateClientOrSkipAsync(AspireAiProvider requiredProvider, string providerName)
    {
        if (AspireTestHost.AiProvider != requiredProvider)
        {
            Assert.Inconclusive(
                $"{providerName} smoke skipped. Active AI provider: {AspireTestHost.AiProvider}.");
            throw new InvalidOperationException("Unreachable");
        }

        var baseUrl = Environment.GetEnvironmentVariable(LiveBaseUrlVariable);
        if (string.IsNullOrWhiteSpace(baseUrl))
            return await CreateAspireGatewayClientAsync();

        var baseUri = new Uri(baseUrl.TrimEnd('/') + "/", UriKind.Absolute);
        var handler = new HttpClientHandler();

        // Aspire dev HTTPS commonly uses a local development certificate. Trust only loopback here.
        if (baseUri.IsLoopback)
            handler.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;

        return new HttpClient(handler)
        {
            BaseAddress = baseUri,
            Timeout = TimeSpan.FromMinutes(3)
        };
    }

    private async Task<HttpClient> CreateAspireGatewayClientAsync()
    {
        var ct = TestContext.CancellationTokenSource.Token;
        await AspireTestHost.WaitForResourceHealthyAsync("taskflowgateway", ct);

        return AspireTestHost.AspireApp!.CreateHttpClient("taskflowgateway", "http");
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

    private static void AssertHasText(JsonElement element, string propertyName)
    {
        Assert.AreEqual(JsonValueKind.String, element.ValueKind, $"{propertyName} should be a JSON string.");
        Assert.IsFalse(string.IsNullOrWhiteSpace(element.GetString()), $"{propertyName} should not be blank.");
    }
}
