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

    public SqlApiFactory(string? applicationStyle = null)
    {
        _applicationStyle = applicationStyle
            ?? Environment.GetEnvironmentVariable(ApplicationStyleResolver.EnvironmentVariable)
            ?? ApplicationStyle.Service.ToString();
    }

    public static async Task StartContainerAsync()
    {
        await Sql.StartAsync();
    }

    public static async Task StopContainerAsync()
    {
        await Sql.DisposeAsync();
    }

    protected override void ConfigureTestConfiguration(IConfigurationBuilder config)
    {
        config.AddInMemoryCollection(new Dictionary<string, string?>
        {
            [ApplicationStyleResolver.ConfigKey] = _applicationStyle
        });
    }

    protected override DbContextOptions BuildTrxnOptions() =>
        BuildSqlServerOptions<TaskFlowDbContextTrxn>(Sql.ConnectionString);

    protected override DbContextOptions BuildQueryOptions() =>
        BuildSqlServerOptions<TaskFlowDbContextQuery>(Sql.ConnectionString);

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
