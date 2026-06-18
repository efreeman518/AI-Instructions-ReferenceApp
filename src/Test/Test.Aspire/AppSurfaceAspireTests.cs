using System.Net;
using Aspire.Hosting.Testing;

namespace Test.Aspire;

/// <summary>
/// Aspire mesh smoke coverage for app-facing resources that the test graph now includes by default.
/// </summary>
[TestClass]
[TestCategory("Integration")]
[DoNotParallelize]
public class AppSurfaceAspireTests
{
    /// <summary>Boots the shared Aspire graph before app-surface checks run.</summary>
    [ClassInitialize]
    public static Task ClassInit(TestContext context) => AspireTestHost.EnsureStartedAsync(context);

    /// <summary>Verifies the Gateway project is part of the default Aspire test graph.</summary>
    [TestMethod]
    [Timeout(300000)]
    public async Task Given_AppHost_When_GatewayRootRequested_Then_GatewayResponds()
    {
        var ct = TestContext.CancellationTokenSource.Token;
        await AspireTestHost.WaitForResourceHealthyAsync("taskflowgateway", ct);

        using var client = AspireTestHost.AspireApp!.CreateHttpClient("taskflowgateway", "http");
        using var response = await client.GetAsync("/", ct);
        var body = await response.Content.ReadAsStringAsync(ct);

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        StringAssert.Contains(body, "TaskFlow Gateway");
    }

    /// <summary>Verifies the Blazor project is part of the default Aspire test graph.</summary>
    [TestMethod]
    [Timeout(300000)]
    public async Task Given_AppHost_When_BlazorRootRequested_Then_BlazorHostResponds()
    {
        var ct = TestContext.CancellationTokenSource.Token;
        await AspireTestHost.WaitForResourceHealthyAsync("taskflowblazor", ct);

        using var client = AspireTestHost.AspireApp!.CreateHttpClient("taskflowblazor", "http");
        using var response = await client.GetAsync("/", ct);
        var body = await response.Content.ReadAsStringAsync(ct);

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        StringAssert.Contains(body, "<html");
    }

    /// <summary>Verifies the React Vite project is included when node modules are present.</summary>
    [TestMethod]
    [Timeout(300000)]
    public async Task Given_AppHost_When_ReactIsRunnable_Then_ReactHostResponds()
    {
        if (!AspireTestHost.ReactAvailable)
            Assert.Inconclusive("React host skipped because node_modules or node runtime was not available.");

        var ct = TestContext.CancellationTokenSource.Token;
        await AspireTestHost.WaitForResourceHealthyAsync("taskflowreact", ct);

        using var client = AspireTestHost.AspireApp!.CreateHttpClient("taskflowreact", "http");
        using var response = await client.GetAsync("/", ct);
        var body = await response.Content.ReadAsStringAsync(ct);

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        StringAssert.Contains(body, "root");
    }

    /// <summary>Verifies the Uno WASM static host is included when built assets are present.</summary>
    [TestMethod]
    [Timeout(300000)]
    public async Task Given_AppHost_When_UnoWasmIsRunnable_Then_UnoHostResponds()
    {
        if (!AspireTestHost.UnoWasmAvailable)
            Assert.Inconclusive("Uno WASM host skipped because built browser assets were not available.");

        var ct = TestContext.CancellationTokenSource.Token;
        await AspireTestHost.WaitForResourceHealthyAsync("taskflowuno", ct);

        using var client = AspireTestHost.AspireApp!.CreateHttpClient("taskflowuno", "http");
        using var response = await client.GetAsync("/", ct);
        var body = await response.Content.ReadAsStringAsync(ct);

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        StringAssert.Contains(body, "<html");
    }

    /// <summary>Gets MSTest context for cancellation.</summary>
    public TestContext TestContext { get; set; } = null!;
}
