using EF.Data.Migrations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TaskFlow.Infrastructure.Data;

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();
builder.Services
    .AddDatabaseMigrationRunner()
    .AddTaskFlowMigrationDbContexts(builder.Configuration)
    // Order is intentional: app schema first, FlowEngine second, Scheduler/TickerQ last.
    // Each target keeps its own migration history table even when local Aspire shares taskflowdb.
    .AddEfCoreMigrationTarget<TaskFlowDbContextTrxn>("TaskFlowDbContextTrxn", 10)
    .AddEfCoreMigrationTarget<TaskFlowFlowEngineDbContext>("TaskFlowFlowEngineDbContext", 20)
    .AddEfCoreMigrationTarget<TaskFlowTickerQDbContext>("TaskFlowTickerQDbContext", 30);

using var host = builder.Build();
using var scope = host.Services.CreateScope();
var runner = scope.ServiceProvider.GetRequiredService<DatabaseMigrationRunner>();
await runner.RunAsync();

static class DatabaseMigratorRegistration
{
    public static IServiceCollection AddTaskFlowMigrationDbContexts(
        this IServiceCollection services,
        IConfiguration config)
    {
        var trxn = RequireConnectionString(config, "TaskFlowDbContextTrxn");
        // FlowEngine and TickerQ can share the same physical database locally, but they keep
        // distinct logical connection names so Azure can split them later through configuration.
        var flowEngine = config.GetConnectionString("TaskFlowFlowEngineDbContext") ?? trxn;
        var tickerQ = RequireConnectionString(config, "TickerQDbContext");

        var retryCount = config.GetValue<int?>("Database:Retry:MaxRetryCount") ?? 5;
        var retryDelaySeconds = config.GetValue<int?>("Database:Retry:MaxRetryDelaySeconds") ?? 30;
        var commandTimeoutSeconds = config.GetValue<int?>("Database:MigrationCommandTimeoutSeconds") ?? 1800;

        services.AddDbContextFactory<TaskFlowDbContextTrxn>(options =>
            ConfigureSqlServer(options, trxn, retryCount, retryDelaySeconds, commandTimeoutSeconds));

        services.AddDbContextFactory<TaskFlowFlowEngineDbContext>(options =>
            ConfigureSqlServer(
                options,
                flowEngine,
                retryCount,
                retryDelaySeconds,
                commandTimeoutSeconds,
                TaskFlowFlowEngineDbContext.MigrationHistoryTable,
                TaskFlowFlowEngineDbContext.SchemaName));

        services.AddDbContextFactory<TaskFlowTickerQDbContext>(options =>
            ConfigureSqlServer(
                options,
                tickerQ,
                retryCount,
                retryDelaySeconds,
                commandTimeoutSeconds,
                TaskFlowTickerQDbContext.MigrationHistoryTable,
                TaskFlowTickerQDbContext.SchemaName,
                typeof(TaskFlowTickerQDbContext).Assembly.GetName().Name));

        return services;
    }

    private static string RequireConnectionString(IConfiguration config, string name)
    {
        return config.GetConnectionString(name)
            ?? throw new InvalidOperationException($"Connection string '{name}' is required.");
    }

    private static void ConfigureSqlServer(
        DbContextOptionsBuilder options,
        string connectionString,
        int retryCount,
        int retryDelaySeconds,
        int commandTimeoutSeconds,
        string? migrationsHistoryTable = null,
        string? migrationsHistorySchema = null,
        string? migrationsAssembly = null)
    {
        var retryDelay = TimeSpan.FromSeconds(retryDelaySeconds);

        void Configure(SqlServerDbContextOptionsBuilder sql)
        {
            sql.UseLatestCompatibilityLevel();
            sql.CommandTimeout(commandTimeoutSeconds);
            sql.EnableRetryOnFailure(retryCount, retryDelay, null);

            if (!string.IsNullOrWhiteSpace(migrationsHistoryTable))
            {
                sql.MigrationsHistoryTable(migrationsHistoryTable, migrationsHistorySchema);
            }

            if (!string.IsNullOrWhiteSpace(migrationsAssembly))
            {
                sql.MigrationsAssembly(migrationsAssembly);
            }
        }

        if (connectionString.Contains("database.windows.net", StringComparison.OrdinalIgnoreCase))
        {
            options.UseAzureSql(connectionString, sql =>
            {
                sql.UseLatestCompatibilityLevel();
                sql.CommandTimeout(commandTimeoutSeconds);
                sql.EnableRetryOnFailure(retryCount, retryDelay, null);

                if (!string.IsNullOrWhiteSpace(migrationsHistoryTable))
                {
                    sql.MigrationsHistoryTable(migrationsHistoryTable, migrationsHistorySchema);
                }

                if (!string.IsNullOrWhiteSpace(migrationsAssembly))
                {
                    sql.MigrationsAssembly(migrationsAssembly);
                }
            });
            return;
        }

        options.UseSqlServer(connectionString, Configure);
    }
}
