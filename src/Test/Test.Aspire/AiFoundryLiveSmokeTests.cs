using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Test.Aspire;

/// <summary>
/// Opt-in smoke tests for the model-backed AI endpoints running through the real Aspire-hosted API or
/// gateway. These are not normal CI tests: they require a live AppHost already started with either
/// Foundry Local or Azure Foundry wiring, and they assert response contracts instead of exact model text.
///
/// Foundry Local setup:
/// 1. Install Foundry Local: <c>winget install Microsoft.FoundryLocal</c>.
/// 2. Start or verify the service: <c>foundry service status</c>.
/// 3. Download the local model used by AppHost: <c>foundry model download qwen2.5-0.5b</c>.
/// 4. From repo root, start AppHost with local model wiring:
///    <c>$env:TASKFLOW_ENABLE_FOUNDRY_LOCAL="true"; dotnet run --project src\Host\Aspire\AppHost\AppHost.csproj</c>.
/// 5. In another PowerShell window, point tests at the Aspire gateway or taskflowapi HTTP/HTTPS endpoint:
///    <c>$env:TASKFLOW_TEST_FOUNDRY_LOCAL="true"; $env:TASKFLOW_LIVE_AI_BASE_URL="https://localhost:51600"; dotnet test src\Test\Test.Aspire\Test.Aspire.csproj --filter "TestCategory=LiveAI&amp;FullyQualifiedName~FoundryLocal"</c>.
///
/// Azure Foundry setup, once enabled:
/// 1. Configure the AppHost Azure Foundry endpoint through user secrets, azd, or environment values.
/// 2. Start AppHost with <c>TASKFLOW_USE_AZURE_FOUNDRY=true</c>.
/// 3. Set <c>TASKFLOW_LIVE_AI_BASE_URL</c> to the gateway or taskflowapi endpoint.
/// 4. Remove the <c>[Ignore]</c> attribute from the Azure smoke test and run:
///    <c>dotnet test src\Test\Test.Aspire\Test.Aspire.csproj --filter "TestCategory=AzureFoundry"</c>.
/// </summary>
[TestClass]
[TestCategory("LiveAI")]
[DoNotParallelize]
public class AiFoundryLiveSmokeTests
{
    private const string LiveBaseUrlVariable = "TASKFLOW_LIVE_AI_BASE_URL";
    private const string FoundryLocalOptInVariable = "TASKFLOW_TEST_FOUNDRY_LOCAL";
    private const string AzureFoundryOptInVariable = "TASKFLOW_TEST_AZURE_FOUNDRY";
    private const string ApiPrefix = "api/v1/";

    /// <summary>Gets MSTest context so opt-in guard messages show in local test output.</summary>
    public TestContext TestContext { get; set; } = null!;

    /// <summary>Verifies D1 reaches a configured local model and returns non-empty assistant text.</summary>
    [TestMethod]
    [TestCategory("FoundryLocal")]
    [Timeout(180000)]
    public async Task Given_FoundryLocalAppHost_When_ChatEndpointCalled_Then_ConfiguredModelResponds()
    {
        using var client = CreateOptInClientOrSkip(FoundryLocalOptInVariable, "Foundry Local");
        if (client is null)
            return;

        await AssertConfiguredChatAsync(client, "Answer in one short sentence: what does TaskFlow track?");
    }

    /// <summary>Verifies D4 can classify a real task through the local model without applying writes.</summary>
    [TestMethod]
    [TestCategory("FoundryLocal")]
    [Timeout(180000)]
    public async Task Given_FoundryLocalAppHost_When_TaskTriageCalled_Then_TriageContractReturned()
    {
        using var client = CreateOptInClientOrSkip(FoundryLocalOptInVariable, "Foundry Local");
        if (client is null)
            return;

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
        using var client = CreateOptInClientOrSkip(FoundryLocalOptInVariable, "Foundry Local");
        if (client is null)
            return;

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
        using var client = CreateOptInClientOrSkip(FoundryLocalOptInVariable, "Foundry Local");
        if (client is null)
            return;

        var ct = TestContext.CancellationTokenSource.Token;
        using var response = await client.PostAsync(ApiPath(client, "api/v1/ai/next-action"), null, ct);
        using var payload = await ReadJsonAsync(response, ct);

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        Assert.IsTrue(payload.RootElement.GetProperty("isConfigured").GetBoolean());
        AssertHasText(payload.RootElement.GetProperty("recommendation"), "recommendation");
    }

    /// <summary>
    /// Azure Foundry smoke stays ignored until the Azure connection contract has been validated end to end.
    /// Once wired, remove <c>[Ignore]</c>, set <c>TASKFLOW_TEST_AZURE_FOUNDRY=true</c>, and point
    /// <c>TASKFLOW_LIVE_AI_BASE_URL</c> at the Aspire gateway or taskflowapi endpoint.
    /// </summary>
    [TestMethod]
    [TestCategory("AzureFoundry")]
    [Ignore("Enable after Azure Foundry endpoint wiring is validated for AppHost run mode.")]
    [Timeout(180000)]
    public async Task Given_AzureFoundryAppHost_When_ChatEndpointCalled_Then_ConfiguredModelResponds()
    {
        using var client = CreateOptInClientOrSkip(AzureFoundryOptInVariable, "Azure Foundry");
        if (client is null)
            return;

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

    private HttpClient? CreateOptInClientOrSkip(string optInVariable, string providerName)
    {
        if (!string.Equals(Environment.GetEnvironmentVariable(optInVariable), "true", StringComparison.OrdinalIgnoreCase))
        {
            TestContext.WriteLine($"{providerName} smoke skipped. Set {optInVariable}=true and {LiveBaseUrlVariable}=<AppHost endpoint> to run it.");
            return null;
        }

        var baseUrl = Environment.GetEnvironmentVariable(LiveBaseUrlVariable);
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            TestContext.WriteLine($"{providerName} smoke skipped. Set {LiveBaseUrlVariable} to the Aspire gateway or taskflowapi endpoint.");
            return null;
        }

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
        var stream = await response.Content.ReadAsStreamAsync(ct);
        return await JsonDocument.ParseAsync(stream, cancellationToken: ct);
    }

    private static void AssertHasText(JsonElement element, string propertyName)
    {
        Assert.AreEqual(JsonValueKind.String, element.ValueKind, $"{propertyName} should be a JSON string.");
        Assert.IsFalse(string.IsNullOrWhiteSpace(element.GetString()), $"{propertyName} should not be blank.");
    }
}
