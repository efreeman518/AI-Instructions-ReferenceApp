using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Aspire.Hosting.Testing;

namespace Test.Aspire;

/// <summary>
/// Aspire mesh tests for the AI integration no-model path. The testing AppHost can now wire Foundry
/// Local or Azure Foundry when available, so these assertions run only when no model was configured.
/// Run this deterministic tier from repo root with:
/// <c>dotnet test src\Test\Test.Aspire\Test.Aspire.csproj --filter FullyQualifiedName~AiAspireIntegrationTests</c>.
/// Use <see cref="AiFoundryLiveSmokeTests"/> for model-backed smoke coverage.
/// </summary>
[TestClass]
[TestCategory("Integration")]
[DoNotParallelize]
public class AiAspireIntegrationTests
{
    /// <summary>Boots the shared Aspire graph before AI endpoint checks run.</summary>
    [ClassInitialize]
    public static Task ClassInit(TestContext context) => AspireTestHost.EnsureStartedAsync(context);

    /// <summary>Verifies AppHost no-model mode still exposes the chat endpoint with an explicit disabled response.</summary>
    [TestMethod]
    [Timeout(300000)]
    public async Task Given_AppHostWithoutFoundry_When_ChatEndpointCalled_Then_NoOpChatClientResponds()
    {
        SkipWhenLiveAiConfigured();

        var ct = CancellationToken.None;
        await AspireTestHost.WaitForResourceHealthyAsync("taskflowapi", ct);

        using var client = AspireTestHost.AspireApp!.CreateHttpClient("taskflowapi", "http");
        using var response = await client.PostAsJsonAsync(
            "/api/v1/ai/chat",
            new { message = "This should use the no-op AI client." },
            ct);

        var payload = await ReadJsonAsync(response, ct);

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        Assert.IsFalse(payload.RootElement.GetProperty("isConfigured").GetBoolean());
        StringAssert.Contains(
            payload.RootElement.GetProperty("message").GetString(),
            "AI model is not configured");
    }

    /// <summary>Verifies write-adjacent AI demo stays inert when no model is wired by Aspire.</summary>
    [TestMethod]
    [Timeout(300000)]
    public async Task Given_AppHostWithoutFoundry_When_DraftTaskCalled_Then_NoTaskIsCreated()
    {
        SkipWhenLiveAiConfigured();

        var ct = CancellationToken.None;
        await AspireTestHost.WaitForResourceHealthyAsync("taskflowapi", ct);

        using var client = AspireTestHost.AspireApp!.CreateHttpClient("taskflowapi", "http");
        using var response = await client.PostAsJsonAsync(
            "/api/v1/ai/tasks/draft",
            new { title = "Draft should not create without AI" },
            ct);

        var payload = await ReadJsonAsync(response, ct);

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        Assert.IsFalse(payload.RootElement.GetProperty("isConfigured").GetBoolean());
        Assert.IsFalse(payload.RootElement.GetProperty("created").GetBoolean());
        Assert.AreEqual(JsonValueKind.Null, payload.RootElement.GetProperty("taskId").ValueKind);
        Assert.AreEqual("AI model not configured.", payload.RootElement.GetProperty("error").GetString());
    }

    private static async Task<JsonDocument> ReadJsonAsync(HttpResponseMessage response, CancellationToken ct)
    {
        var stream = await response.Content.ReadAsStreamAsync(ct);
        return await JsonDocument.ParseAsync(stream, cancellationToken: ct);
    }

    private static void SkipWhenLiveAiConfigured()
    {
        if (AspireTestHost.AiProvider != AspireAiProvider.None)
        {
            Assert.Inconclusive(
                $"No-model AI assertion skipped because the Aspire test graph is using {AspireTestHost.AiProvider}.");
        }
    }
}
