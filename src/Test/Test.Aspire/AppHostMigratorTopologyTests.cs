namespace Test.Aspire;

[TestClass]
[TestCategory("Aspire")]
public sealed class AppHostMigratorTopologyTests
{
    [TestMethod]
    public void AppHost_WiresDatabaseMigratorBeforeRuntimeHosts()
    {
        var appHostSource = ReadAppHostSource();

        StringAssert.Contains(appHostSource, "TaskFlow_DatabaseMigrator");
        StringAssert.Contains(appHostSource, "connectionName: \"TaskFlowDbContextTrxn\"");
        StringAssert.Contains(appHostSource, "connectionName: \"TaskFlowFlowEngineDbContext\"");
        StringAssert.Contains(appHostSource, "connectionName: \"TickerQDbContext\"");
        StringAssert.Contains(appHostSource, ".WaitForCompletion(migrator)");
    }

    [TestMethod]
    public void AppHost_PreservesFoundryLocalTestingOptInWiring()
    {
        var appHostSource = ReadAppHostSource();

        StringAssert.Contains(appHostSource, "TASKFLOW_ASPIRE_ENABLE_FOUNDRY_LOCAL");
        StringAssert.Contains(appHostSource, ".WithEnvironment(\"AiServices__DisableFoundryLocal\", \"false\")");
        StringAssert.Contains(appHostSource, ".WithEnvironment(\"AiServices__RequireFoundryLocal\", \"true\")");
        StringAssert.Contains(appHostSource, "TASKFLOW_FOUNDRY_LOCAL_MODEL");
        StringAssert.Contains(appHostSource, "TASKFLOW_FOUNDRY_LOCAL_WEB_URL");
    }

    [TestMethod]
    public void TestAspire_UsesSingleSharedDistributedAppBuilder()
    {
        var repoRoot = FindRepoRoot();
        var testAspireRoot = Path.Combine(repoRoot, "src", "Test", "Test.Aspire");
        var builderCall = "DistributedApplicationTestingBuilder." + "CreateAsync";
        var matches = Directory
            .EnumerateFiles(testAspireRoot, "*.cs", SearchOption.AllDirectories)
            .Select(path => new
            {
                Path = path,
                Count = File.ReadAllText(path)
                    .Split(builderCall).Length - 1
            })
            .Where(match => match.Count > 0)
            .ToArray();

        Assert.AreEqual(1, matches.Sum(match => match.Count), string.Join(Environment.NewLine, matches.Select(match => match.Path)));
        Assert.IsTrue(matches[0].Path.EndsWith(Path.Combine("Test.Aspire", "AspireTestHost.cs"), StringComparison.Ordinal), matches[0].Path);
    }

    private static string ReadAppHostSource() =>
        File.ReadAllText(Path.Combine(
            FindRepoRoot(),
            "src",
            "Host",
            "Aspire",
            "AppHost",
            "AppHost.cs"));

    private static string FindRepoRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "src", "TaskFlow.slnx")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        Assert.Inconclusive("Could not locate repository root.");
        throw new InvalidOperationException("Unreachable.");
    }
}
