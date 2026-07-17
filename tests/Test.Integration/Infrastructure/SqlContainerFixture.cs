using EF.IntegrationTesting.Testcontainers;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using TaskFlow.Infrastructure.Data;

namespace Test.Integration.Infrastructure;

/// <summary>
/// Standalone SQL Server Testcontainer for the component tier. Wraps the shared EF.IntegrationTesting
/// <c>MsSqlContainerFixture</c> so SQL-only repository/migration/projection tests run against a real
/// database without booting the Aspire AppHost graph. Started once by <see cref="IntegrationTestSetup"/>;
/// <see cref="StartupError"/> is captured so dependent tests fail with its diagnostics without aborting
/// assembly discovery.
/// </summary>
internal static class SqlContainerFixture
{
    private static readonly MsSqlContainerFixture Sql = new();

    /// <summary>Startup failure captured by <see cref="StartAsync"/>; null when the container started cleanly.</summary>
    internal static Exception? StartupError { get; private set; }

    /// <summary>Connection string for the running SQL container. Only valid once startup succeeded.</summary>
    internal static string ConnectionString => Sql.ConnectionString;

    /// <summary>Starts the SQL container, capturing any post-preflight failure for dependent tests.</summary>
    internal static async Task StartAsync()
    {
        try
        {
            await Sql.StartAsync();
        }
        catch (Exception ex)
        {
            StartupError = ex;
        }
    }

    /// <summary>Disposes the SQL container.</summary>
    internal static async Task StopAsync() => await Sql.DisposeAsync();

    /// <summary>Creates an empty isolated database and returns a connection string pointing to it.</summary>
    internal static async Task<string> CreateEmptyDatabaseConnectionStringAsync(string prefix)
    {
        var builder = new SqlConnectionStringBuilder(Sql.ConnectionString);
        var databaseName = $"{prefix}_{Guid.NewGuid():N}";
        var masterBuilder = new SqlConnectionStringBuilder(builder.ConnectionString)
        {
            InitialCatalog = "master"
        };

        await using var connection = new SqlConnection(masterBuilder.ConnectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = $"CREATE DATABASE [{databaseName}]";
        await command.ExecuteNonQueryAsync();

        builder.InitialCatalog = databaseName;
        return builder.ConnectionString;
    }

    /// <summary>Builds a trxn context against the standalone SQL container.</summary>
    internal static TaskFlowDbContextTrxn CreateTrxnContext(string? connString = null) =>
        new(BuildSqlServerOptions<TaskFlowDbContextTrxn>(connString ?? Sql.ConnectionString)) { AuditId = "integration-test" };

    /// <summary>Builds a trxn context using the pre-schema-pinning dbo migration history location.</summary>
    internal static TaskFlowDbContextTrxn CreateLegacyTrxnContext(string connectionString)
    {
        var options = new DbContextOptionsBuilder<TaskFlowDbContextTrxn>()
            .UseSqlServer(connectionString, sql =>
            {
                sql.UseLatestCompatibilityLevel();
                sql.EnableRetryOnFailure();
            })
            .Options;

        return new TaskFlowDbContextTrxn(options) { AuditId = "integration-test" };
    }

    /// <summary>Builds a query context against the standalone SQL container.</summary>
    internal static TaskFlowDbContextQuery CreateQueryContext(string? connString = null) =>
        new(BuildSqlServerOptions<TaskFlowDbContextQuery>(connString ?? Sql.ConnectionString)) { AuditId = "integration-test" };

    /// <summary>Builds a FlowEngine context against the standalone SQL container.</summary>
    internal static TaskFlowFlowEngineDbContext CreateFlowEngineContext(string? connString = null)
    {
        var options = new DbContextOptionsBuilder<TaskFlowFlowEngineDbContext>()
            .UseSqlServer(connString ?? Sql.ConnectionString, sql =>
            {
                sql.UseLatestCompatibilityLevel();
                sql.EnableRetryOnFailure();
                sql.MigrationsHistoryTable(
                    TaskFlowFlowEngineDbContext.MigrationHistoryTable,
                    TaskFlowFlowEngineDbContext.SchemaName);
            })
            .Options;

        return new TaskFlowFlowEngineDbContext(options);
    }

    /// <summary>Builds a TickerQ context against the standalone SQL container.</summary>
    internal static TaskFlowTickerQDbContext CreateTickerQContext(string? connString = null)
    {
        var options = new DbContextOptionsBuilder<TaskFlowTickerQDbContext>()
            .UseSqlServer(connString ?? Sql.ConnectionString, sql =>
            {
                sql.UseLatestCompatibilityLevel();
                sql.EnableRetryOnFailure();
                sql.MigrationsAssembly(typeof(TaskFlowTickerQDbContext).Assembly.GetName().Name);
                sql.MigrationsHistoryTable(
                    TaskFlowTickerQDbContext.MigrationHistoryTable,
                    TaskFlowTickerQDbContext.SchemaName);
            })
            .Options;

        return new TaskFlowTickerQDbContext(options);
    }

    /// <summary>Builds SQL Server options used by focused test cases.</summary>
    private static DbContextOptions<TContext> BuildSqlServerOptions<TContext>(string connectionString)
        where TContext : DbContext =>
        new DbContextOptionsBuilder<TContext>()
            .UseSqlServer(connectionString, sql =>
            {
                sql.UseLatestCompatibilityLevel();
                sql.EnableRetryOnFailure();
                sql.MigrationsHistoryTable(
                    TaskFlowDbContextBase.MigrationHistoryTable,
                    TaskFlowDbContextBase.SchemaName);
            })
            .Options;
}
