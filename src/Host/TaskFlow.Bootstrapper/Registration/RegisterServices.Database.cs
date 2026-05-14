using EF.Data;
using EF.Data.Interceptors;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TaskFlow.Application.Contracts.Repositories;
using TaskFlow.Infrastructure.Data;
using TaskFlow.Infrastructure.Repositories;

namespace TaskFlow.Bootstrapper;

public static partial class RegisterServices
{
    private static void AddDatabaseServices(IServiceCollection services, IConfiguration config)
    {
        services.AddTransient<AuditInterceptor<string, Guid?>>();
        services.AddTransient<ConnectionNoLockInterceptor>();

        var dbConnectionStringTrxn = config.GetConnectionString("TaskFlowDbContextTrxn") ?? "";
        var dbConnectionStringQuery = config.GetConnectionString("TaskFlowDbContextQuery") ?? "";
        var maxRetryCount = config.GetValue<int?>("Database:Retry:MaxRetryCount") ?? 5;
        var maxRetryDelaySeconds = config.GetValue<int?>("Database:Retry:MaxRetryDelaySeconds") ?? 30;

        services.AddPooledDbContextFactory<TaskFlowDbContextTrxn>((sp, options) =>
        {
            ConfigureSqlOptions(options, dbConnectionStringTrxn, maxRetryCount, maxRetryDelaySeconds);
            var auditInterceptor = sp.GetRequiredService<AuditInterceptor<string, Guid?>>();
            options.AddInterceptors(auditInterceptor);
        });
        services.AddScoped<DbContextScopedFactory<TaskFlowDbContextTrxn, string, Guid?>>();
        services.AddScoped(sp => sp.GetRequiredService<DbContextScopedFactory<TaskFlowDbContextTrxn, string, Guid?>>()
            .CreateDbContext());

        services.AddPooledDbContextFactory<TaskFlowDbContextQuery>((sp, options) =>
        {
            var readOnlyConnStr = dbConnectionStringQuery.Contains("ApplicationIntent=")
                ? dbConnectionStringQuery
                : dbConnectionStringQuery + ";ApplicationIntent=ReadOnly";
            options.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
            ConfigureSqlOptions(options, readOnlyConnStr, maxRetryCount, maxRetryDelaySeconds);
        });
        services.AddScoped<DbContextScopedFactory<TaskFlowDbContextQuery, string, Guid?>>();
        services.AddScoped(sp => sp.GetRequiredService<DbContextScopedFactory<TaskFlowDbContextQuery, string, Guid?>>()
            .CreateDbContext());

        // FlowEngine DbContext — same SQL Server connection, separate schema + migration history.
        // Inherits FlowEngineOutboxDbContext to enable atomic state+outbox saves.
        services.AddPooledDbContextFactory<TaskFlowFlowEngineDbContext>((sp, options) =>
        {
            ConfigureFlowEngineSqlOptions(options, dbConnectionStringTrxn, maxRetryCount, maxRetryDelaySeconds);
        });

        services.AddScoped<ICategoryRepositoryTrxn, CategoryRepositoryTrxn>();
        services.AddScoped<ICategoryRepositoryQuery, CategoryRepositoryQuery>();
        services.AddScoped<ITagRepositoryTrxn, TagRepositoryTrxn>();
        services.AddScoped<ITagRepositoryQuery, TagRepositoryQuery>();
        services.AddScoped<ITaskItemRepositoryTrxn, TaskItemRepositoryTrxn>();
        services.AddScoped<ITaskItemRepositoryQuery, TaskItemRepositoryQuery>();
        services.AddScoped<ICommentRepositoryTrxn, CommentRepositoryTrxn>();
        services.AddScoped<ICommentRepositoryQuery, CommentRepositoryQuery>();
        services.AddScoped<IChecklistItemRepositoryTrxn, ChecklistItemRepositoryTrxn>();
        services.AddScoped<IChecklistItemRepositoryQuery, ChecklistItemRepositoryQuery>();
        services.AddScoped<IAttachmentRepositoryTrxn, AttachmentRepositoryTrxn>();
        services.AddScoped<IAttachmentRepositoryQuery, AttachmentRepositoryQuery>();
        services.AddScoped<ITaskItemTagRepositoryTrxn, TaskItemTagRepositoryTrxn>();
        services.AddScoped<ITaskItemTagRepositoryQuery, TaskItemTagRepositoryQuery>();
    }

    private static void ConfigureSqlOptions(
        DbContextOptionsBuilder options,
        string connectionString,
        int maxRetryCount,
        int maxRetryDelaySeconds)
    {
        if (string.IsNullOrEmpty(connectionString)) return;

        var maxRetryDelay = TimeSpan.FromSeconds(maxRetryDelaySeconds);

        if (connectionString.Contains("database.windows.net"))
        {
            options.UseAzureSql(connectionString, sqlOptions =>
            {
                sqlOptions.UseCompatibilityLevel(170);
                sqlOptions.EnableRetryOnFailure(maxRetryCount: maxRetryCount,
                    maxRetryDelay: maxRetryDelay, errorNumbersToAdd: null);
            });
        }
        else
        {
            options.UseSqlServer(connectionString, sqlOptions =>
            {
                sqlOptions.UseCompatibilityLevel(160);
                sqlOptions.EnableRetryOnFailure(maxRetryCount: maxRetryCount,
                    maxRetryDelay: maxRetryDelay, errorNumbersToAdd: null);
            });
        }
    }

    // FlowEngine variant: isolates migration history to a dedicated table so it does not
    // collide with the application's __EFMigrationsHistory.
    private static void ConfigureFlowEngineSqlOptions(
        DbContextOptionsBuilder options,
        string connectionString,
        int maxRetryCount,
        int maxRetryDelaySeconds)
    {
        if (string.IsNullOrEmpty(connectionString)) return;

        var maxRetryDelay = TimeSpan.FromSeconds(maxRetryDelaySeconds);

        if (connectionString.Contains("database.windows.net"))
        {
            options.UseAzureSql(connectionString, sqlOptions =>
            {
                sqlOptions.UseCompatibilityLevel(170);
                sqlOptions.EnableRetryOnFailure(maxRetryCount: maxRetryCount,
                    maxRetryDelay: maxRetryDelay, errorNumbersToAdd: null);
                sqlOptions.MigrationsHistoryTable(
                    TaskFlowFlowEngineDbContext.MigrationHistoryTable,
                    TaskFlowFlowEngineDbContext.SchemaName);
            });
        }
        else
        {
            options.UseSqlServer(connectionString, sqlOptions =>
            {
                sqlOptions.UseCompatibilityLevel(160);
                sqlOptions.EnableRetryOnFailure(maxRetryCount: maxRetryCount,
                    maxRetryDelay: maxRetryDelay, errorNumbersToAdd: null);
                sqlOptions.MigrationsHistoryTable(
                    TaskFlowFlowEngineDbContext.MigrationHistoryTable,
                    TaskFlowFlowEngineDbContext.SchemaName);
            });
        }
    }
}
