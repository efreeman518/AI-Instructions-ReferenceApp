using Microsoft.EntityFrameworkCore;
using TaskFlow.Infrastructure.Data;
using Testcontainers.MsSql;

namespace Test.Integration;

[TestClass]
public class DatabaseFixture
{
    private static MsSqlContainer _container = null!;
    internal static string ConnectionString = null!;

    [AssemblyInitialize]
    public static async Task AssemblyInit(TestContext _)
    {
        _container = new MsSqlBuilder()
            .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
            .Build();

        await _container.StartAsync();
        ConnectionString = _container.GetConnectionString();
    }

    [AssemblyCleanup]
    public static async Task AssemblyCleanup()
    {
        await _container.DisposeAsync();
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
