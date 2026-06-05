namespace Test.Integration.Infrastructure;

/// <summary>
/// Assembly-scoped lifecycle for the component tier. Starts the standalone SQL and Azurite
/// Testcontainers in parallel via <c>[AssemblyInitialize]</c> and disposes them via
/// <c>[AssemblyCleanup]</c>. Each fixture captures its own <c>StartupError</c> (assembly-init safety) so a
/// container failure marks only the dependent tests Inconclusive instead of aborting the whole assembly.
/// Component tier only - no Aspire graph, no <c>AppHost</c> reference.
/// </summary>
[TestClass]
public class IntegrationTestSetup
{
    /// <summary>Starts the component-tier store containers in parallel before any test runs.</summary>
    [AssemblyInitialize]
    public static async Task AssemblyInit(TestContext _) =>
        await Task.WhenAll(
            SqlContainerFixture.StartAsync(),
            AzuriteContainerFixture.StartAsync());

    /// <summary>Disposes the component-tier store containers after the assembly's tests complete.</summary>
    [AssemblyCleanup]
    public static async Task AssemblyCleanup(TestContext _) =>
        await Task.WhenAll(
            SqlContainerFixture.StopAsync(),
            AzuriteContainerFixture.StopAsync());
}
