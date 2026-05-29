using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace TaskFlow.Infrastructure.Data;

/// <summary>Provides design time DB context factory trxn behavior for the Infrastructure layer.</summary>
[ExcludeFromCodeCoverage]
public class DesignTimeDbContextFactoryTrxn : IDesignTimeDbContextFactory<TaskFlowDbContextTrxn>
{
    /// <summary>Creates requested data after validation and maps the result to the caller contract.</summary>
    public TaskFlowDbContextTrxn CreateDbContext(string[] args)
    {
        string? connString = Environment.GetEnvironmentVariable("EFCORETOOLSDB");
        if (string.IsNullOrEmpty(connString))
            throw new InvalidOperationException("The connection string was not set in the 'EFCORETOOLSDB' environment variable.");

        Console.WriteLine(connString);
        var optionsBuilder = new DbContextOptionsBuilder<TaskFlowDbContextTrxn>();
        optionsBuilder.UseSqlServer(connString, sql => sql.UseLatestCompatibilityLevel());
        return new TaskFlowDbContextTrxn(optionsBuilder.Options) { AuditId = "DesignTimeAuditId", TenantId = Guid.NewGuid() };
    }
}

/// <summary>Carries design time DB context factory query CQRS data between endpoints and handlers.</summary>
[ExcludeFromCodeCoverage]
public class DesignTimeDbContextFactoryQuery : IDesignTimeDbContextFactory<TaskFlowDbContextQuery>
{
    /// <summary>Creates requested data after validation and maps the result to the caller contract.</summary>
    public TaskFlowDbContextQuery CreateDbContext(string[] args)
    {
        string? connString = Environment.GetEnvironmentVariable("EFCORETOOLSDB");
        if (string.IsNullOrEmpty(connString))
            throw new InvalidOperationException("The connection string was not set in the 'EFCORETOOLSDB' environment variable.");

        Console.WriteLine(connString);
        var optionsBuilder = new DbContextOptionsBuilder<TaskFlowDbContextQuery>();
        optionsBuilder.UseSqlServer(connString, sql => sql.UseLatestCompatibilityLevel());
        return new TaskFlowDbContextQuery(optionsBuilder.Options) { AuditId = "DesignTimeAuditId", TenantId = Guid.NewGuid() };
    }
}

/// <summary>Provides design time DB context factory flow engine behavior for the Infrastructure layer.</summary>
[ExcludeFromCodeCoverage]
public class DesignTimeDbContextFactoryFlowEngine : IDesignTimeDbContextFactory<TaskFlowFlowEngineDbContext>
{
    /// <summary>Creates requested data after validation and maps the result to the caller contract.</summary>
    public TaskFlowFlowEngineDbContext CreateDbContext(string[] args)
    {
        string? connString = Environment.GetEnvironmentVariable("EFCORETOOLSDB");
        if (string.IsNullOrEmpty(connString))
            throw new InvalidOperationException("The connection string was not set in the 'EFCORETOOLSDB' environment variable.");

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
