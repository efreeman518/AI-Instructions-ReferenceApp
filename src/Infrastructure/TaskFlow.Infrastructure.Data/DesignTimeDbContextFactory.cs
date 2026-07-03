using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using System.Diagnostics.CodeAnalysis;

namespace TaskFlow.Infrastructure.Data;

/// <summary>Creates the transactional context for EF tooling.</summary>
[ExcludeFromCodeCoverage]
public class DesignTimeDbContextFactoryTrxn : IDesignTimeDbContextFactory<TaskFlowDbContextTrxn>
{
    public TaskFlowDbContextTrxn CreateDbContext(string[] args)
    {
        var connString = DesignTimeConnectionStrings.Require("EFCORETOOLSDB");
        var optionsBuilder = new DbContextOptionsBuilder<TaskFlowDbContextTrxn>();
        optionsBuilder.UseSqlServer(connString, sql => sql.UseLatestCompatibilityLevel());
        return new TaskFlowDbContextTrxn(optionsBuilder.Options)
        {
            AuditId = "DesignTimeAuditId",
            TenantId = Guid.NewGuid()
        };
    }
}

/// <summary>Creates the query context for EF tooling.</summary>
[ExcludeFromCodeCoverage]
public class DesignTimeDbContextFactoryQuery : IDesignTimeDbContextFactory<TaskFlowDbContextQuery>
{
    public TaskFlowDbContextQuery CreateDbContext(string[] args)
    {
        var connString = DesignTimeConnectionStrings.Require("EFCORETOOLSDB");
        var optionsBuilder = new DbContextOptionsBuilder<TaskFlowDbContextQuery>();
        optionsBuilder.UseSqlServer(connString, sql => sql.UseLatestCompatibilityLevel());
        return new TaskFlowDbContextQuery(optionsBuilder.Options)
        {
            AuditId = "DesignTimeAuditId",
            TenantId = Guid.NewGuid()
        };
    }
}

/// <summary>Creates the FlowEngine context for EF tooling.</summary>
[ExcludeFromCodeCoverage]
public class DesignTimeDbContextFactoryFlowEngine : IDesignTimeDbContextFactory<TaskFlowFlowEngineDbContext>
{
    public TaskFlowFlowEngineDbContext CreateDbContext(string[] args)
    {
        var connString = Environment.GetEnvironmentVariable("EFCORETOOLSDB_FLOWENGINE")
            ?? DesignTimeConnectionStrings.Require("EFCORETOOLSDB");
        var optionsBuilder = new DbContextOptionsBuilder<TaskFlowFlowEngineDbContext>();
        optionsBuilder.UseSqlServer(connString, sql =>
        {
            sql.UseLatestCompatibilityLevel();
            sql.MigrationsHistoryTable(
                TaskFlowFlowEngineDbContext.MigrationHistoryTable,
                TaskFlowFlowEngineDbContext.SchemaName);
        });
        return new TaskFlowFlowEngineDbContext(optionsBuilder.Options);
    }
}

/// <summary>Creates the TickerQ context for EF tooling.</summary>
[ExcludeFromCodeCoverage]
public class DesignTimeDbContextFactoryTickerQ : IDesignTimeDbContextFactory<TaskFlowTickerQDbContext>
{
    public TaskFlowTickerQDbContext CreateDbContext(string[] args)
    {
        var connString = Environment.GetEnvironmentVariable("EFCORETOOLSDB_TICKERQ")
            ?? DesignTimeConnectionStrings.Require("EFCORETOOLSDB");
        var optionsBuilder = new DbContextOptionsBuilder<TaskFlowTickerQDbContext>();
        optionsBuilder.UseSqlServer(connString, sql =>
        {
            sql.UseLatestCompatibilityLevel();
            sql.MigrationsAssembly(typeof(TaskFlowTickerQDbContext).Assembly.GetName().Name);
            sql.MigrationsHistoryTable(
                TaskFlowTickerQDbContext.MigrationHistoryTable,
                TaskFlowTickerQDbContext.SchemaName);
        });
        return new TaskFlowTickerQDbContext(optionsBuilder.Options);
    }
}

file static class DesignTimeConnectionStrings
{
    public static string Require(string environmentVariableName)
    {
        return Environment.GetEnvironmentVariable(environmentVariableName)
            ?? throw new InvalidOperationException(
                $"The connection string was not set in '{environmentVariableName}' environment variable.");
    }
}
