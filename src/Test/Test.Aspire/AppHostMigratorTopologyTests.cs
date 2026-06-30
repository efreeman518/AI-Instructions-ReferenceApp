namespace Test.Aspire;

[TestClass]
[TestCategory("Aspire")]
public sealed class AppHostMigratorTopologyTests
{
    [TestMethod]
    public void AppHost_WiresDatabaseMigratorBeforeRuntimeHosts()
    {
        var appHostSource = File.ReadAllText(Path.Combine(
            FindRepoRoot(),
            "src",
            "Host",
            "Aspire",
            "AppHost",
            "AppHost.cs"));

        StringAssert.Contains(appHostSource, "TaskFlow_DatabaseMigrator");
        StringAssert.Contains(appHostSource, "connectionName: \"TaskFlowDbContextTrxn\"");
        StringAssert.Contains(appHostSource, "connectionName: \"TaskFlowFlowEngineDbContext\"");
        StringAssert.Contains(appHostSource, "connectionName: \"TickerQDbContext\"");
        StringAssert.Contains(appHostSource, ".WaitForCompletion(migrator)");
    }

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
