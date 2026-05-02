using Aspire.Hosting;
using Aspire.Hosting.Testing;
using Microsoft.EntityFrameworkCore;
using AppHost;
using TaskFlow.Infrastructure.Data;

namespace Test.Integration;

[TestClass]
public class DatabaseFixture
{
    private static string? _originalSqlPassword;
    private static string? _originalAspireTesting;
    private static string? _originalIncludeFunctions;
    internal static string ConnectionString = null!;

    /// <summary>Shared Aspire app started once for all Aspire-based integration tests.</summary>
    internal static DistributedApplication? AspireApp { get; private set; }

    [AssemblyInitialize]
    public static async Task AssemblyInit(TestContext _)
    {
        // SQL password for Aspire AppHost SQL resource
        _originalSqlPassword = Environment.GetEnvironmentVariable("Parameters__sql-password");
        Environment.SetEnvironmentVariable("Parameters__sql-password", LocalSqlSettings.SharedSaPassword);

        // Shared Aspire environment — started once, reused across all Aspire test classes
        _originalAspireTesting = Environment.GetEnvironmentVariable("TASKFLOW_ASPIRE_TESTING");
        _originalIncludeFunctions = Environment.GetEnvironmentVariable("TASKFLOW_INCLUDE_FUNCTIONS");

        Environment.SetEnvironmentVariable("TASKFLOW_ASPIRE_TESTING", "true");
        if (EnsureFuncToolAvailable())
            Environment.SetEnvironmentVariable("TASKFLOW_INCLUDE_FUNCTIONS", "true");

        var appHostProgramType = Type.GetType("Program, AppHost", throwOnError: true)!;
        var builder = await DistributedApplicationTestingBuilder.CreateAsync(appHostProgramType);
        AspireApp = await builder.BuildAsync();
        await AspireApp.StartAsync();

        var sqlConnectionString = await AspireApp.GetConnectionStringAsync("taskflowdb");
        ConnectionString = string.IsNullOrWhiteSpace(sqlConnectionString)
            ? throw new InvalidOperationException("Aspire SQL connection string 'taskflowdb' was not resolved.")
            : sqlConnectionString;
    }

    [AssemblyCleanup]
    public static async Task AssemblyCleanup()
    {
        if (AspireApp is not null)
            await AspireApp.DisposeAsync();

        Environment.SetEnvironmentVariable("TASKFLOW_ASPIRE_TESTING", _originalAspireTesting);
        Environment.SetEnvironmentVariable("TASKFLOW_INCLUDE_FUNCTIONS", _originalIncludeFunctions);
        Environment.SetEnvironmentVariable("Parameters__sql-password", _originalSqlPassword);
    }

    /// <summary>
    /// Checks if Azure Functions Core Tools (func.exe) is available on PATH.
    /// Mutates PATH to include the discovery location on Windows if found via LocalAppData fallback.
    /// </summary>
    internal static bool EnsureFuncToolAvailable()
    {
        var candidateNames = OperatingSystem.IsWindows()
            ? new[] { "func.exe", "func.cmd", "func.bat" }
            : new[] { "func" };

        var path = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
        var pathEntries = path.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        foreach (var directory in pathEntries)
            foreach (var candidate in candidateNames)
                if (File.Exists(Path.Combine(directory, candidate)))
                    return true;

        if (!OperatingSystem.IsWindows())
            return false;

        var localFunctionsToolsRoot = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "AzureFunctionsTools",
            "Releases");

        if (!Directory.Exists(localFunctionsToolsRoot))
            return false;

        var discoveredDirectory = Directory
            .EnumerateFiles(localFunctionsToolsRoot, "func.exe", SearchOption.AllDirectories)
            .Select(Path.GetDirectoryName)
            .Where(directory => !string.IsNullOrWhiteSpace(directory))
            .Select(directory => directory!)
            .OrderByDescending(directory => directory.Contains("4.", StringComparison.OrdinalIgnoreCase))
            .ThenByDescending(directory => directory, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault();

        if (discoveredDirectory == null)
            return false;

        Environment.SetEnvironmentVariable(
            "PATH",
            string.IsNullOrWhiteSpace(path)
                ? discoveredDirectory
                : string.Join(Path.PathSeparator, [discoveredDirectory, .. pathEntries]));

        return true;
    }

    internal static TaskFlowDbContextTrxn CreateTrxnContext(string? connString = null)
    {
        var options = new DbContextOptionsBuilder<TaskFlowDbContextTrxn>()
            .UseSqlServer(connString ?? ConnectionString)
            .Options;
        return new TaskFlowDbContextTrxn(options) { AuditId = "integration-test" };
    }

    internal static TaskFlowDbContextQuery CreateQueryContext(string? connString = null)
    {
        var options = new DbContextOptionsBuilder<TaskFlowDbContextQuery>()
            .UseSqlServer(connString ?? ConnectionString)
            .Options;
        return new TaskFlowDbContextQuery(options) { AuditId = "integration-test" };
    }
}
