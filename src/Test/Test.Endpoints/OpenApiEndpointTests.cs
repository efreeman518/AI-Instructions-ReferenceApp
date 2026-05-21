using System.Net;
using System.Text.Json;

namespace Test.Endpoints;

[TestClass]
public sealed class OpenApiEndpointTests
{
    private static CustomApiFactory _factory = null!;

    [ClassInitialize]
    public static void ClassInit(TestContext _) => _factory = new CustomApiFactory();

    [ClassCleanup]
    public static void ClassCleanup() => _factory?.Dispose();

    [TestCategory("Endpoint")]
    [TestMethod]
    public async Task Given_OpenApiEnabled_When_GetV1Document_Then_ContainsVersionedDomainRoutes()
    {
        using var client = _factory.CreateClient();

        using var response = await client.GetAsync("/openapi/v1.json");

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
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
}
