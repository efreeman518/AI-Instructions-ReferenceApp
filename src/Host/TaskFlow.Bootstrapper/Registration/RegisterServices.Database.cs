using Azure.Identity;
using EF.Data;
using EF.Data.Contracts;
using EF.Data.Interceptors;
using Microsoft.Data.SqlClient;
using Microsoft.Data.SqlClient.AlwaysEncrypted.AzureKeyVaultProvider;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TaskFlow.Application.Contracts.Repositories;
using TaskFlow.Infrastructure.Data;
using TaskFlow.Infrastructure.Repositories;

namespace TaskFlow.Bootstrapper;

/// <summary>Configures database services for TaskFlow runtime hosts.</summary>
public static partial class RegisterServices
{
    /// <summary>
    /// Registers write DbContext, read DbContext, FlowEngine DbContext, and repositories.
    /// </summary>
    private static void AddDatabaseServices(IServiceCollection services, IConfiguration config)
    {
        services.AddTransient<AuditInterceptor<string, Guid?>>();
        services.AddTransient<ConnectionNoLockInterceptor>();

        var dbConnectionStringTrxn = config.GetConnectionString("TaskFlowDbContextTrxn") ?? "";
        var dbConnectionStringQuery = config.GetConnectionString("TaskFlowDbContextQuery") ?? "";
        var dbConnectionStringFlowEngine =
            config.GetConnectionString("TaskFlowFlowEngineDbContext") ?? dbConnectionStringTrxn;
        var maxRetryCount = config.GetValue<int?>("Database:Retry:MaxRetryCount") ?? 5;
        var maxRetryDelaySeconds = config.GetValue<int?>("Database:Retry:MaxRetryDelaySeconds") ?? 30;

        // Always Encrypted (D-019) is opt-in. When enabled, register the Azure Key Vault CMK provider once and
        // turn on client-side column encryption for the contexts that map TaskItem (Trxn + Query). Off by
        // default so local build/test/run need no Key Vault.
        var alwaysEncrypted = config.GetValue<bool?>("Database:AlwaysEncrypted:Enabled")
            ?? string.Equals(
                Environment.GetEnvironmentVariable("TASKFLOW_ENABLE_ALWAYS_ENCRYPTED"),
                "true",
                StringComparison.OrdinalIgnoreCase);
        if (alwaysEncrypted)
        {
            RegisterSqlAlwaysEncrypted();
            dbConnectionStringTrxn = EnsureColumnEncryption(dbConnectionStringTrxn);
            dbConnectionStringQuery = EnsureColumnEncryption(dbConnectionStringQuery);
        }

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

        services.AddPooledDbContextFactory<TaskFlowFlowEngineDbContext>((sp, options) =>
        {
            ConfigureFlowEngineSqlOptions(
                options,
                dbConnectionStringFlowEngine,
                maxRetryCount,
                maxRetryDelaySeconds);
        });

        services.AddScoped(typeof(IRepositoryTrxn<,>), typeof(TaskFlowRepositoryTrxn<,>));
        services.AddScoped(typeof(IRepositoryQuery<,>), typeof(TaskFlowRepositoryQuery<,>));

        services.AddScoped<ICategoryRepositoryTrxn, CategoryRepositoryTrxn>();
        services.AddScoped<ICategoryRepositoryQuery, CategoryRepositoryQuery>();
        services.AddScoped<ITaskItemRepositoryTrxn, TaskItemRepositoryTrxn>();
        services.AddScoped<ITaskItemRepositoryQuery, TaskItemRepositoryQuery>();
        services.AddScoped<IAttachmentRepositoryTrxn, AttachmentRepositoryTrxn>();
        services.AddScoped<IAttachmentRepositoryQuery, AttachmentRepositoryQuery>();

        services.AddScoped<ITagRepositoryQuery, TagRepositoryQuery>();
        services.AddScoped<ICommentRepositoryQuery, CommentRepositoryQuery>();
        services.AddScoped<IChecklistItemRepositoryQuery, ChecklistItemRepositoryQuery>();
    }

    private static bool _keyStoreProviderRegistered;

    // Registers the Azure Key Vault CMK provider for Always Encrypted (D-019). SQL Always Encrypted also
    // requires "Column Encryption Setting=Enabled" on the connection string (see EnsureColumnEncryption).
    private static void RegisterSqlAlwaysEncrypted()
    {
        if (!_keyStoreProviderRegistered)
        {
            var credential = new DefaultAzureCredential();
            SqlColumnEncryptionAzureKeyVaultProvider sqlColumnEncryptionAzureKeyVaultProvider = new(credential);

            try
            {
                SqlConnection.RegisterColumnEncryptionKeyStoreProviders(
                    customProviders: new Dictionary<string, SqlColumnEncryptionKeyStoreProvider>(capacity: 1, comparer: StringComparer.OrdinalIgnoreCase)
                    {
                        {
                            SqlColumnEncryptionAzureKeyVaultProvider.ProviderName,
                            sqlColumnEncryptionAzureKeyVaultProvider
                        }
                    });
            }
            catch
            {
                // Ignore; already registered. SqlConnection has no try-register or "is registered" check, and
                // the provider can be registered only once per process (e.g. across WebApplicationFactory reuse).
            }
            _keyStoreProviderRegistered = true;
        }
    }

    private static string EnsureColumnEncryption(string connectionString) =>
        string.IsNullOrEmpty(connectionString)
            || connectionString.Contains("Column Encryption Setting", StringComparison.OrdinalIgnoreCase)
            ? connectionString
            : connectionString + ";Column Encryption Setting=Enabled";

    private static void ConfigureSqlOptions(
        DbContextOptionsBuilder options,
        string connectionString,
        int maxRetryCount,
        int maxRetryDelaySeconds)
    {
        if (string.IsNullOrEmpty(connectionString)) return;

        var maxRetryDelay = TimeSpan.FromSeconds(maxRetryDelaySeconds);

        if (connectionString.Contains("database.windows.net", StringComparison.OrdinalIgnoreCase))
        {
            options.UseAzureSql(connectionString, sqlOptions =>
            {
                sqlOptions.UseLatestCompatibilityLevel();
                sqlOptions.EnableRetryOnFailure(
                    maxRetryCount: maxRetryCount,
                    maxRetryDelay: maxRetryDelay,
                    errorNumbersToAdd: null);
                sqlOptions.MigrationsHistoryTable(
                    TaskFlowDbContextBase.MigrationHistoryTable,
                    TaskFlowDbContextBase.SchemaName);
            });
        }
        else
        {
            options.UseSqlServer(connectionString, sqlOptions =>
            {
                sqlOptions.UseLatestCompatibilityLevel();
                sqlOptions.EnableRetryOnFailure(
                    maxRetryCount: maxRetryCount,
                    maxRetryDelay: maxRetryDelay,
                    errorNumbersToAdd: null);
                sqlOptions.MigrationsHistoryTable(
                    TaskFlowDbContextBase.MigrationHistoryTable,
                    TaskFlowDbContextBase.SchemaName);
            });
        }
    }

    private static void ConfigureFlowEngineSqlOptions(
        DbContextOptionsBuilder options,
        string connectionString,
        int maxRetryCount,
        int maxRetryDelaySeconds)
    {
        if (string.IsNullOrEmpty(connectionString)) return;

        var maxRetryDelay = TimeSpan.FromSeconds(maxRetryDelaySeconds);

        if (connectionString.Contains("database.windows.net", StringComparison.OrdinalIgnoreCase))
        {
            options.UseAzureSql(connectionString, sqlOptions =>
            {
                sqlOptions.UseLatestCompatibilityLevel();
                sqlOptions.EnableRetryOnFailure(
                    maxRetryCount: maxRetryCount,
                    maxRetryDelay: maxRetryDelay,
                    errorNumbersToAdd: null);
                sqlOptions.MigrationsHistoryTable(
                    TaskFlowFlowEngineDbContext.MigrationHistoryTable,
                    TaskFlowFlowEngineDbContext.SchemaName);
            });
        }
        else
        {
            options.UseSqlServer(connectionString, sqlOptions =>
            {
                sqlOptions.UseLatestCompatibilityLevel();
                sqlOptions.EnableRetryOnFailure(
                    maxRetryCount: maxRetryCount,
                    maxRetryDelay: maxRetryDelay,
                    errorNumbersToAdd: null);
                sqlOptions.MigrationsHistoryTable(
                    TaskFlowFlowEngineDbContext.MigrationHistoryTable,
                    TaskFlowFlowEngineDbContext.SchemaName);
            });
        }
    }
}
