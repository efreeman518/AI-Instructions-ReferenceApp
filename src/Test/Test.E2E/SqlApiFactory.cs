using Microsoft.EntityFrameworkCore;
using TaskFlow.Infrastructure.Data;
using Test.Support;
using Testcontainers.MsSql;

namespace Test.E2E;

/// <summary>
/// Real-SQL-Server WebApplicationFactory backed by Testcontainers.
/// Exercises the full stack: HTTP → Endpoint → Service → EF → SQL.
///
/// Used for multi-endpoint workflow E2E tests where contract-only endpoint coverage (Test.Endpoints'
/// in-memory factory) is insufficient — e.g., tests that span create → search → update → delete and
/// need real SQL behavior (concurrency, projection plans, FK constraints).
/// </summary>
public sealed class SqlApiFactory : WebApplicationFactoryBase<Program, TaskFlowDbContextTrxn, TaskFlowDbContextQuery>
{
    private static MsSqlContainer _container = null!;
    private static string _connectionString = null!;
    private static bool _started;

    public static async Task StartContainerAsync()
    {
        if (_started) return;
        _container = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest").Build();
        await _container.StartAsync();
        _connectionString = _container.GetConnectionString();
        _started = true;
    }

    public static async Task StopContainerAsync()
    {
        if (!_started) return;
        await _container.DisposeAsync();
        _started = false;
    }

    protected override DbContextOptions BuildTrxnOptions() =>
        new DbContextOptionsBuilder<TaskFlowDbContextTrxn>().UseSqlServer(_connectionString).Options;

    protected override DbContextOptions BuildQueryOptions() =>
        new DbContextOptionsBuilder<TaskFlowDbContextQuery>().UseSqlServer(_connectionString).Options;
}
