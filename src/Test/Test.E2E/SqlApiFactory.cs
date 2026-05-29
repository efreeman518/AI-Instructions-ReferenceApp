using EF.IntegrationTesting.Testcontainers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using TaskFlow.Application.Contracts;
using TaskFlow.Infrastructure.Data;
using Test.Support;

namespace Test.E2E;

/// <summary>
/// Real-SQL-Server WebApplicationFactory backed by Testcontainers.
/// Exercises the full stack: HTTP -> endpoint style -> application layer -> EF -> SQL.
/// Set TASKFLOW_APPLICATION_STYLE=Cqrs to run the same workflow tests against CQRS endpoint mappings.
/// </summary>
public sealed class SqlApiFactory : WebApplicationFactoryBase<Program, TaskFlowDbContextTrxn, TaskFlowDbContextQuery>
{
    private static readonly MsSqlContainerFixture Sql = new();

    private readonly string _applicationStyle;

    /// <summary>Initializes SQL API factory with required dependencies and default state.</summary>
    public SqlApiFactory(string? applicationStyle = null)
    {
        _applicationStyle = applicationStyle
            ?? Environment.GetEnvironmentVariable(ApplicationStyleResolver.EnvironmentVariable)
            ?? ApplicationStyle.Service.ToString();
    }

    /// <summary>Verifies start container behavior and protects the expected test contract.</summary>
    public static async Task StartContainerAsync()
    {
        await Sql.StartAsync();
    }

    /// <summary>Verifies stop container behavior and protects the expected test contract.</summary>
    public static async Task StopContainerAsync()
    {
        await Sql.DisposeAsync();
    }

    /// <summary>Verifies configure test configuration behavior and protects the expected test contract.</summary>
    protected override void ConfigureTestConfiguration(IConfigurationBuilder config)
    {
        config.AddInMemoryCollection(new Dictionary<string, string?>
        {
            [ApplicationStyleResolver.ConfigKey] = _applicationStyle
        });
    }

    /// <summary>Builds trxn options used by focused test cases.</summary>
    protected override DbContextOptions BuildTrxnOptions() =>
        BuildSqlServerOptions<TaskFlowDbContextTrxn>(Sql.ConnectionString);

    /// <summary>Builds query options used by focused test cases.</summary>
    protected override DbContextOptions BuildQueryOptions() =>
        BuildSqlServerOptions<TaskFlowDbContextQuery>(Sql.ConnectionString);

    /// <summary>Builds SQL server options used by focused test cases.</summary>
    private static DbContextOptions<TContext> BuildSqlServerOptions<TContext>(string connectionString)
        where TContext : DbContext =>
        new DbContextOptionsBuilder<TContext>()
            .UseSqlServer(connectionString, sql =>
            {
                sql.UseLatestCompatibilityLevel();
                sql.EnableRetryOnFailure();
            })
            .Options;
}
