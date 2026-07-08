using System.Net;
using System.Text.Json;

namespace Test.Endpoints;

/// <summary>Covers open API endpoint behavior with focused assertions that document expected behavior and regression intent.</summary>
[TestClass]
public sealed class OpenApiEndpointTests
{
    private static CustomApiFactory _factory = null!;

    /// <summary>Initializes shared test fixtures before the class-level test run begins.</summary>
    [ClassInitialize]
    public static void ClassInit(TestContext _) => _factory = new CustomApiFactory();

    /// <summary>Disposes shared test fixtures after the class-level test run finishes.</summary>
    [ClassCleanup]
    public static void ClassCleanup() => _factory?.Dispose();

    /// <summary>Verifies that given open API enabled, when get v 1 document, then contains versioned domain routes.</summary>
    [TestCategory("Endpoint")]
    [TestMethod]
    public async Task Given_OpenApiEnabled_When_GetV1Document_Then_ContainsVersionedDomainRoutes()
    {
        using var client = _factory.CreateClient();

        using var response = await client.GetAsync("/openapi/v1.json", TestContext.CancellationToken);

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(TestContext.CancellationToken));
        Assert.AreEqual("v1", document.RootElement.GetProperty("info").GetProperty("version").GetString());

        var paths = document.RootElement.GetProperty("paths")
            .EnumerateObject()
            .Select(path => path.Name)
            .ToArray();

        Assert.IsTrue(
            paths.Any(path => path.StartsWith("/api/v1/task-items", StringComparison.Ordinal)),
            "OpenAPI v1 document must include versioned domain API routes.");

        Assert.IsFalse(
            paths.Any(path => path.StartsWith("/health", StringComparison.Ordinal)
                || path.StartsWith("/alive", StringComparison.Ordinal)
                || path.StartsWith("/api/flowengine", StringComparison.Ordinal)),
            "OpenAPI v1 document should not include unversioned operational/admin routes.");
    }

    public TestContext TestContext { get; set; } = null!;
}
