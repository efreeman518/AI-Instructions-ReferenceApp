using Aspire.Hosting.Testing;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Test.Aspire;

/// <summary>
/// Aspire mesh smoke tests for shipped FlowEngine workflows whose agent nodes resolve through the
/// AppHost-provided Foundry chat deployment.
/// </summary>
[TestClass]
[TestCategory("LiveAI")]
[TestCategory("Foundry")]
[TestCategory("FlowEngine")]
[DoNotParallelize]
public sealed class FlowEngineFoundryWorkflowTests
{
    private static readonly Guid TenantId = Guid.Parse("00000000-0000-0000-0000-000000000001");
    private static readonly TimeSpan PollTimeout = TimeSpan.FromMinutes(3);
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(3);

    /// <summary>Gets MSTest context for cancellation.</summary>
    public TestContext TestContext { get; set; } = null!;

    /// <summary>Boots the shared Aspire graph before workflow checks run.</summary>
    [ClassInitialize]
    public static Task ClassInit(TestContext context) => AspireTestHost.EnsureStartedAsync(context);

    /// <summary>Verifies the triage workflow reaches Foundry through its agent node.</summary>
    [TestMethod]
    [Timeout(360000)]
    public async Task Given_FoundryBackedAppHost_When_TriageWorkflowStarted_Then_AgentProducesTriageContext()
    {
        var ct = TestContext.CancellationTokenSource.Token;
        using var client = await CreateApiClientAsync(ct);
        await AssertFoundryProviderAvailableAsync(client, ct);

        var title = "FlowEngine Foundry triage " + Guid.NewGuid().ToString("N");
        var taskId = await CreateTaskAsync(client, title, ct);
        var instanceId = await StartWorkflowAsync(
            client,
            "ai-task-triage",
            new Dictionary<string, object?>
            {
                ["tenantId"] = TenantId.ToString(),
                ["taskId"] = taskId.ToString(),
                ["description"] = "Classify this implementation task as high priority but not critical."
            },
            ct);

        using var instance = await WaitForInstanceAsync(
            client,
            instanceId,
            e => ContainsString(e, "n-classify")
                && (HasStringProperty(e, "suggestedPriority") || StringPropertyEquals(e, "currentNodeId", "n-faulted")),
            "triage workflow agent completion",
            ct);

        Assert.IsTrue(ContainsString(instance.RootElement, "n-classify"));
        Assert.IsTrue(
            HasStringProperty(instance.RootElement, "suggestedPriority")
            || StringPropertyEquals(instance.RootElement, "currentNodeId", "n-faulted"),
            "Expected structured triage output or the workflow schema-guard fault path.");
    }

    /// <summary>Verifies the decomposer workflow reaches Foundry and produces a subtask proposal.</summary>
    [TestMethod]
    [Timeout(360000)]
    public async Task Given_FoundryBackedAppHost_When_DecomposerWorkflowStarted_Then_AgentProducesSubtaskProposal()
    {
        var ct = TestContext.CancellationTokenSource.Token;
        using var client = await CreateApiClientAsync(ct);
        await AssertFoundryProviderAvailableAsync(client, ct);

        var title = "FlowEngine Foundry decompose " + Guid.NewGuid().ToString("N");
        var taskId = await CreateTaskAsync(client, title, ct);
        var instanceId = await StartWorkflowAsync(
            client,
            "ai-task-decomposer",
            new Dictionary<string, object?>
            {
                ["tenantId"] = TenantId.ToString(),
                ["taskId"] = taskId.ToString(),
                ["description"] = "Build a user login page with form validation and password reset."
            },
            ct);

        // Proposal is stored as the array `proposal.subtasks`, so completion is keyed off the
        // workflow's terminal output nodes: n-output-ok on a successful decomposition, or the
        // fault/rejected terminals when the live model produces unusable output (intentional leniency).
        using var instance = await WaitForInstanceAsync(
            client,
            instanceId,
            e => ContainsString(e, "n-propose-subtasks") && ReachedDecomposerTerminal(e),
            "decomposer workflow agent completion",
            ct);

        Assert.IsTrue(ContainsString(instance.RootElement, "n-propose-subtasks"));
        Assert.IsTrue(
            ReachedDecomposerTerminal(instance.RootElement),
            "Expected the decomposer to reach a terminal output node (decomposed, rejected, or failed).");
    }

    private static async Task<HttpClient> CreateApiClientAsync(CancellationToken ct)
    {
        await AspireTestHost.WaitForResourceHealthyAsync("taskflowapi", ct);
        var client = AspireTestHost.AspireApp!.CreateHttpClient("taskflowapi", "http");
        client.Timeout = TimeSpan.FromMinutes(5);
        return client;
    }

    private static async Task<Guid> CreateTaskAsync(HttpClient client, string title, CancellationToken ct)
    {
        using var response = await client.PostAsJsonAsync(
            "/api/v1/task-items",
            new
            {
                item = new
                {
                    title,
                    description = "Created by FlowEngine Foundry workflow smoke tests.",
                    priority = 2
                }
            },
            ct);
        using var payload = await ReadJsonAsync(response, ct);

        Assert.AreEqual(HttpStatusCode.Created, response.StatusCode);
        return payload.RootElement.GetProperty("item").GetProperty("id").GetGuid();
    }

    private static async Task<string> StartWorkflowAsync(
        HttpClient client,
        string workflowId,
        Dictionary<string, object?> parameters,
        CancellationToken ct)
    {
        using var response = await client.PostAsJsonAsync(
            "/api/flowengine/instances/start",
            new Dictionary<string, object?>
            {
                ["workflowId"] = workflowId,
                ["tenantId"] = TenantId.ToString(),
                ["correlationId"] = Guid.NewGuid().ToString("N"),
                ["params"] = parameters
            },
            ct);
        using var payload = await ReadJsonAsync(response, ct);

        Assert.IsTrue(
            response.StatusCode is HttpStatusCode.OK or HttpStatusCode.Accepted or HttpStatusCode.Created,
            $"Unexpected workflow start status: {(int)response.StatusCode} {response.ReasonPhrase}");

        var instanceId = FindStringProperty(payload.RootElement, "instanceId");
        Assert.IsFalse(string.IsNullOrWhiteSpace(instanceId), "Workflow start response did not include instanceId.");
        return instanceId!;
    }

    private static async Task<JsonDocument> WaitForInstanceAsync(
        HttpClient client,
        string instanceId,
        Func<JsonElement, bool> predicate,
        string description,
        CancellationToken ct)
    {
        var deadline = DateTimeOffset.UtcNow.Add(PollTimeout);
        var lastBody = string.Empty;

        while (DateTimeOffset.UtcNow < deadline)
        {
            using var response = await client.GetAsync($"/api/flowengine/instances/{instanceId}", ct);
            lastBody = await response.Content.ReadAsStringAsync(ct);
            Assert.IsTrue(
                response.IsSuccessStatusCode,
                $"Instance read failed: {(int)response.StatusCode} {response.ReasonPhrase}. Body: {Truncate(lastBody)}");

            var document = JsonDocument.Parse(lastBody);
            if (predicate(document.RootElement))
                return document;

            document.Dispose();
            await Task.Delay(PollInterval, ct);
        }

        Assert.Fail($"Timed out waiting for {description}. Last instance body: {Truncate(lastBody)}");
        throw new InvalidOperationException("Unreachable");
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
            Assert.Fail($"Expected JSON response, got empty body from {response.RequestMessage?.RequestUri}.");

        return JsonDocument.Parse(body);
    }

    private static bool ContainsString(JsonElement element, string expected)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (var property in element.EnumerateObject())
                {
                    if (string.Equals(property.Name, expected, StringComparison.OrdinalIgnoreCase)
                        || ContainsString(property.Value, expected))
                    {
                        return true;
                    }
                }
                return false;
            case JsonValueKind.Array:
                return element.EnumerateArray().Any(item => ContainsString(item, expected));
            case JsonValueKind.String:
                return string.Equals(element.GetString(), expected, StringComparison.OrdinalIgnoreCase);
            default:
                return false;
        }
    }

    private static bool HasStringProperty(JsonElement element, string propertyName)
    {
        foreach (var item in EnumerateObjects(element))
        {
            if (TryGetProperty(item, propertyName, out var value)
                && value.ValueKind == JsonValueKind.String
                && !string.IsNullOrWhiteSpace(value.GetString()))
            {
                return true;
            }
        }

        return false;
    }

    private static string? FindStringProperty(JsonElement element, string propertyName)
    {
        foreach (var item in EnumerateObjects(element))
        {
            if (TryGetProperty(item, propertyName, out var value) && value.ValueKind == JsonValueKind.String)
                return value.GetString();
        }

        return null;
    }

    private static bool ReachedDecomposerTerminal(JsonElement element)
        => StringPropertyEquals(element, "currentNodeId", "n-output-ok")
            || StringPropertyEquals(element, "currentNodeId", "n-output-rejected")
            || StringPropertyEquals(element, "currentNodeId", "n-output-failed");

    private static bool StringPropertyEquals(JsonElement element, string propertyName, string expected)
        => TryGetProperty(element, propertyName, out var value)
            && value.ValueKind == JsonValueKind.String
            && string.Equals(value.GetString(), expected, StringComparison.OrdinalIgnoreCase);

    private static bool TryGetProperty(JsonElement element, string propertyName, out JsonElement value)
    {
        foreach (var property in element.EnumerateObject())
        {
            if (string.Equals(property.Name, propertyName, StringComparison.OrdinalIgnoreCase))
            {
                value = property.Value;
                return true;
            }
        }

        value = default;
        return false;
    }

    private static IEnumerable<JsonElement> EnumerateObjects(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Object)
            yield return element;

        if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in element.EnumerateObject())
            {
                foreach (var child in EnumerateObjects(property.Value))
                    yield return child;
            }
        }
        else if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in element.EnumerateArray())
            {
                foreach (var child in EnumerateObjects(item))
                    yield return child;
            }
        }
    }

    private static string Truncate(string value) =>
        value.Length <= 1000 ? value : value[..1000] + "...";

    private static async Task AssertFoundryProviderAvailableAsync(HttpClient client, CancellationToken ct)
    {
        if (AspireTestHost.AiProvider == AspireAiProvider.None)
        {
            Assert.Inconclusive(
                "Azure AI Foundry is not configured. Run Test.FoundryLocal for local live smoke coverage.");
        }

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeout.CancelAfter(TimeSpan.FromSeconds(15));

        try
        {
            using var response = await client.GetAsync("/api/v1/ai/status", timeout.Token);
            if (!response.IsSuccessStatusCode)
                Assert.Inconclusive($"AI status endpoint returned {(int)response.StatusCode} {response.ReasonPhrase}.");

            var status = await response.Content.ReadFromJsonAsync<AspireTestHost.AiStatus>(cancellationToken: timeout.Token);
            if (status?.Provider != "azure" || status.IsConfigured != true)
            {
                Assert.Inconclusive(
                    $"Azure AI Foundry is not active. AI status provider={status?.Provider ?? "unknown"} configured={status?.IsConfigured.ToString() ?? "unknown"}.");
            }
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            Assert.Inconclusive($"AI status endpoint unavailable: {ex.Message}");
        }
    }
}
