using Aspire.Hosting.Testing;
using System.Net;

namespace Test.Aspire;

/// <summary>
/// Aspire mesh smoke coverage for app-facing resources that the test graph now includes by default.
/// </summary>
[TestClass]
[TestCategory("Aspire")]
[DoNotParallelize]
public class AppSurfaceAspireTests
{
    /// <summary>Boots the shared Aspire graph before app-surface checks run.</summary>
    [ClassInitialize]
    public static Task ClassInit(TestContext context) => AspireTestHost.EnsureStartedAsync(context);

    /// <summary>Verifies the Gateway project is part of the default Aspire test graph.</summary>
    [TestMethod]
    [Timeout(1_200_000, CooperativeCancellation = true)]
    public async Task Given_AppHost_When_GatewayRootRequested_Then_GatewayResponds()
    {
        var ct = TestContext.CancellationToken;
        await AspireTestHost.WaitForResourceHealthyAsync("taskflowgateway", ct);

        using var client = AspireTestHost.AspireApp!.CreateHttpClient("taskflowgateway", "http");
        using var response = await client.GetAsync("/", ct);
        var body = await response.Content.ReadAsStringAsync(ct);

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("TaskFlow Gateway", body);
    }

    /// <summary>Verifies the Blazor project is part of the default Aspire test graph.</summary>
    [TestMethod]
    [Timeout(1_200_000, CooperativeCancellation = true)]
    public async Task Given_AppHost_When_BlazorRootRequested_Then_BlazorHostResponds()
    {
        var ct = TestContext.CancellationToken;
        await AspireTestHost.WaitForResourceHealthyAsync("taskflowblazor", ct);

        using var client = AspireTestHost.AspireApp!.CreateHttpClient("taskflowblazor", "http");
        using var response = await client.GetAsync("/", ct);
        var body = await response.Content.ReadAsStringAsync(ct);

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("<html", body);
    }

    /// <summary>Verifies the React Vite project is included when node modules are present.</summary>
    [TestMethod]
    [Timeout(1_200_000, CooperativeCancellation = true)]
    public async Task Given_AppHost_When_ReactIsRunnable_Then_ReactHostResponds()
    {
        if (!AspireTestHost.ReactAvailable)
        {
            if (IsExplicitlyDisabled("TASKFLOW_REACT_TESTS_ENABLED"))
                Assert.Inconclusive("TASKFLOW_REACT_TESTS_ENABLED=false - React full-stack smoke opted out.");

            Assert.Fail("React host prerequisites are missing. Run npm ci in src/UI/TaskFlow.React or set TASKFLOW_REACT_TESTS_ENABLED=false to opt out explicitly.");
        }

        var ct = TestContext.CancellationToken;
        await AspireTestHost.WaitForResourceHealthyAsync("taskflowreact", ct);

        using var client = AspireTestHost.AspireApp!.CreateHttpClient("taskflowreact", "http");
        using var response = await client.GetAsync("/", ct);
        var body = await response.Content.ReadAsStringAsync(ct);

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("root", body);
    }

    /// <summary>Verifies the Uno WASM static host is included when built assets are present.</summary>
    [TestMethod]
    [Timeout(1_200_000, CooperativeCancellation = true)]
    public async Task Given_AppHost_When_UnoWasmIsRunnable_Then_UnoHostResponds()
    {
        if (!AspireTestHost.UnoWasmAvailable)
        {
            if (IsExplicitlyDisabled("TASKFLOW_WASM_TESTS_ENABLED"))
                Assert.Inconclusive("TASKFLOW_WASM_TESTS_ENABLED=false - Uno WASM full-stack smoke opted out.");

            Assert.Fail("Uno WASM assets are missing. Build the browserwasm target or set TASKFLOW_WASM_TESTS_ENABLED=false to opt out explicitly.");
        }

        var ct = TestContext.CancellationToken;
        await AspireTestHost.WaitForResourceHealthyAsync("taskflowuno", ct);

        using var client = AspireTestHost.AspireApp!.CreateHttpClient("taskflowuno", "http");
        using var response = await client.GetAsync("/", ct);
        var body = await response.Content.ReadAsStringAsync(ct);

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("<html", body);
    }

    /// <summary>Gets MSTest context for cancellation.</summary>
    public TestContext TestContext { get; set; } = null!;

    private static bool IsExplicitlyDisabled(string variableName) =>
        string.Equals(Environment.GetEnvironmentVariable(variableName), "false", StringComparison.OrdinalIgnoreCase);
}
