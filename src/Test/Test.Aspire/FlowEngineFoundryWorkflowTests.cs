using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Aspire.Hosting.Testing;

namespace Test.Aspire;

/// <summary>
/// Aspire mesh smoke tests for shipped FlowEngine workflows whose agent nodes resolve through the
/// AppHost-provided Foundry Local chat deployment.
/// </summary>
[TestClass]
[TestCategory("LiveAI")]
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

    /// <summary>Verifies seeded agent workflow definitions are available through the admin API.</summary>
    [TestMethod]
    [TestCategory("FoundryLocal")]
    [Timeout(300000)]
    public async Task Given_FoundryLocalAppHost_When_WorkflowsListed_Then_AgentWorkflowsAreActive()
    {
        SkipUnlessFoundryLocal();

        var ct = TestContext.CancellationTokenSource.Token;
        using var client = await CreateGatewayClientAsync(ct);
        using var response = await client.GetAsync("/api/flowengine/workflows", ct);
        using var payload = await ReadJsonAsync(response, ct);

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        AssertWorkflowIsActive(payload.RootElement, "ai-task-triage");
        AssertWorkflowIsActive(payload.RootElement, "ai-task-decomposer");
        AssertWorkflowIsActive(payload.RootElement, "compliance-check");
    }

    /// <summary>Verifies the triage workflow reaches Foundry through its agent node.</summary>
    [TestMethod]
    [TestCategory("FoundryLocal")]
    [Timeout(360000)]
    public async Task Given_FoundryLocalAppHost_When_TriageWorkflowStarted_Then_AgentProducesTriageContext()
    {
        SkipUnlessFoundryLocal();

        var ct = TestContext.CancellationTokenSource.Token;
        using var client = await CreateGatewayClientAsync(ct);
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
            e => ContainsString(e, "n-classify") && HasStringProperty(e, "suggestedPriority"),
            "triage workflow agent output",
            ct);

        Assert.IsTrue(ContainsString(instance.RootElement, "n-classify"));
        Assert.IsTrue(HasStringProperty(instance.RootElement, "suggestedPriority"));
    }

    /// <summary>Verifies the decomposer workflow reaches Foundry through its agent node.</summary>
    [TestMethod]
    [TestCategory("FoundryLocal")]
    [Timeout(360000)]
    public async Task Given_FoundryLocalAppHost_When_DecomposerWorkflowStarted_Then_AgentProducesSubtaskContext()
    {
        SkipUnlessFoundryLocal();

        var ct = TestContext.CancellationTokenSource.Token;
        using var client = await CreateGatewayClientAsync(ct);
        var title = "FlowEngine Foundry decomposition " + Guid.NewGuid().ToString("N");
        var taskId = await CreateTaskAsync(client, title, ct);
        var instanceId = await StartWorkflowAsync(
            client,
            "ai-task-decomposer",
            new Dictionary<string, object?>
            {
                ["tenantId"] = TenantId.ToString(),
                ["taskId"] = taskId.ToString(),
                ["description"] = "Plan a migration, update tests, validate telemetry, and publish release notes.",
                ["requireApproval"] = false
            },
            ct);

        using var instance = await WaitForInstanceAsync(
            client,
            instanceId,
            e => ContainsString(e, "n-propose-subtasks") && HasArrayProperty(e, "subtasks"),
            "decomposer workflow agent output",
            ct);

        Assert.IsTrue(ContainsString(instance.RootElement, "n-propose-subtasks"));
        Assert.IsTrue(HasArrayProperty(instance.RootElement, "subtasks"));
    }

    private static async Task<HttpClient> CreateGatewayClientAsync(CancellationToken ct)
    {
        await AspireTestHost.WaitForResourceHealthyAsync("taskflowgateway", ct);
        return AspireTestHost.AspireApp!.CreateHttpClient("taskflowgateway", "http");
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

    private static void AssertWorkflowIsActive(JsonElement root, string workflowId)
    {
        foreach (var workflow in EnumerateObjects(root))
        {
            if (!StringPropertyEquals(workflow, "id", workflowId))
                continue;

            Assert.IsTrue(StringPropertyEquals(workflow, "status", "Active"), $"{workflowId} is not Active.");
            return;
        }

        Assert.Fail($"Workflow {workflowId} was not returned by the admin API.");
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

    private static bool HasArrayProperty(JsonElement element, string propertyName)
    {
        foreach (var item in EnumerateObjects(element))
        {
            if (TryGetProperty(item, propertyName, out var value)
                && value.ValueKind == JsonValueKind.Array
                && value.GetArrayLength() > 0)
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

    private static void SkipUnlessFoundryLocal()
    {
        if (AspireTestHost.AiProvider != AspireAiProvider.FoundryLocal)
        {
            Assert.Inconclusive(
                $"Foundry Local workflow smoke skipped. Active AI provider: {AspireTestHost.AiProvider}.");
        }
    }
}
