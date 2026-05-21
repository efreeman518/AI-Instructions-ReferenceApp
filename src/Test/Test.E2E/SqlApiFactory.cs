using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using TaskFlow.Application.Contracts;
using TaskFlow.Infrastructure.Data;
using Test.Support;
using Testcontainers.MsSql;

namespace Test.E2E;

/// <summary>
/// Real-SQL-Server WebApplicationFactory backed by Testcontainers.
/// Exercises the full stack: HTTP -> endpoint style -> application layer -> EF -> SQL.
/// Set TASKFLOW_APPLICATION_STYLE=Cqrs to run the same workflow tests against CQRS endpoint mappings.
/// </summary>
public sealed class SqlApiFactory : WebApplicationFactoryBase<Program, TaskFlowDbContextTrxn, TaskFlowDbContextQuery>
{
    private static MsSqlContainer _container = null!;
    private static string _connectionString = null!;
    private static bool _started;

    private readonly string _applicationStyle;

    public SqlApiFactory(string? applicationStyle = null)
    {
        _applicationStyle = applicationStyle
            ?? Environment.GetEnvironmentVariable(ApplicationStyleResolver.EnvironmentVariable)
            ?? ApplicationStyle.Service.ToString();
    }

    public static async Task StartContainerAsync()
    {
        if (_started) return;
        _container = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2025-latest").Build();
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

    protected override void ConfigureTestConfiguration(IConfigurationBuilder config)
    {
        config.AddInMemoryCollection(new Dictionary<string, string?>
        {
            [ApplicationStyleResolver.ConfigKey] = _applicationStyle
        });
    }

    protected override DbContextOptions BuildTrxnOptions() =>
        new DbContextOptionsBuilder<TaskFlowDbContextTrxn>().UseSqlServer(_connectionString).Options;

    protected override DbContextOptions BuildQueryOptions() =>
        new DbContextOptionsBuilder<TaskFlowDbContextQuery>().UseSqlServer(_connectionString).Options;
}
