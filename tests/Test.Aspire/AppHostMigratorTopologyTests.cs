namespace Test.Aspire;

[TestClass]
[TestCategory("Aspire")]
public sealed class AppHostMigratorTopologyTests
{
    [TestMethod]
    public void AppHost_WiresDatabaseMigratorBeforeRuntimeHosts()
    {
        var appHostSource = ReadAppHostSource();

        Assert.Contains("TaskFlow_DatabaseMigrator", appHostSource);
        Assert.Contains("connectionName: \"TaskFlowDbContextTrxn\"", appHostSource);
        Assert.Contains("connectionName: \"TaskFlowFlowEngineDbContext\"", appHostSource);
        Assert.Contains("connectionName: \"TickerQDbContext\"", appHostSource);
        Assert.Contains(".WaitForCompletion(migrator)", appHostSource);
    }

    [TestMethod]
    public void AppHost_PreservesFoundryLocalTestingOptInWiring()
    {
        var appHostSource = ReadAppHostSource();

        Assert.Contains("TASKFLOW_ASPIRE_ENABLE_FOUNDRY_LOCAL", appHostSource);
        Assert.Contains(".WithEnvironment(\"AiServices__DisableFoundryLocal\", \"false\")", appHostSource);
        Assert.Contains(".WithEnvironment(\"AiServices__RequireFoundryLocal\", \"true\")", appHostSource);
        Assert.Contains("TASKFLOW_FOUNDRY_LOCAL_MODEL", appHostSource);
        Assert.Contains("TASKFLOW_FOUNDRY_LOCAL_WEB_URL", appHostSource);
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
