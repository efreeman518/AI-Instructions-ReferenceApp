namespace Test.Aspire;

/// <summary>
/// Assembly lifecycle for the mesh tier. The graph is started lazily by
/// <c>AspireTestHost.EnsureStartedAsync</c> from each mesh test class's <c>[ClassInitialize]</c>, so the
/// <c>[AssemblyInitialize]</c> here is intentionally a no-op (keeps the ~60-90 s boot off assemblies that
/// happen to load this one). <c>[AssemblyCleanup]</c> stops and disposes the graph exactly once,
/// regardless of which mesh class warmed it up.
/// </summary>
[TestClass]
public static class AspireMeshLifecycle
{
    /// <summary>No-op: the Aspire graph starts lazily on first mesh-test class, not at assembly load.</summary>
    [AssemblyInitialize]
    public static void AssemblyInit(TestContext _) { }

    /// <summary>Stops and disposes the lazily-started Aspire graph after the assembly's tests complete.</summary>
    [AssemblyCleanup]
    public static Task AssemblyCleanup(TestContext context) => AspireTestHost.StopAsync(context.CancellationToken);
}
