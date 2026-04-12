using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Serialization;
using EF.Common.Contracts;
using EF.Data;
using EF.Data.Interceptors;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TaskFlow.Application.Contracts;
using TaskFlow.Application.Contracts.Repositories;
using TaskFlow.Application.Contracts.Services;
using TaskFlow.Application.Services;
using TaskFlow.Bootstrapper.HealthChecks;
using TaskFlow.Infrastructure.Data;
using TaskFlow.Infrastructure.Repositories;
using ZiggyCreatures.Caching.Fusion;
using ZiggyCreatures.Caching.Fusion.Backplane.StackExchangeRedis;
using ZiggyCreatures.Caching.Fusion.Serialization.SystemTextJson;

namespace TaskFlow.Bootstrapper;

public static class RegisterServices
{
    public static IServiceCollection AddTaskFlowServices(
        this IServiceCollection services, IConfiguration config)
    {
        AddRequestContext(services);
        AddDatabaseServices(services, config);
        AddCachingServices(services, config);
        AddHealthChecks(services);
        AddApplicationServices(services);

        return services;
    }

    private static void AddRequestContext(IServiceCollection services)
    {
        services.AddHttpContextAccessor();

        // Build IRequestContext from authenticated claims (works for both Scaffold and real Entra tokens)
        services.AddScoped<IRequestContext<string, Guid?>>(sp =>
        {
            var httpContextAccessor = sp.GetRequiredService<IHttpContextAccessor>();
            var user = httpContextAccessor.HttpContext?.User;

            if (user?.Identity?.IsAuthenticated != true)
            {
                // Fallback for non-HTTP contexts (background jobs, tests)
                return new RequestContext<string, Guid?>(
                    "system",
                    Guid.NewGuid().ToString(),
                    Guid.Parse("00000000-0000-0000-0000-000000000001"),
                    new List<string>());
            }

            // Claim extraction precedence: oid > NameIdentifier > sub
            var userId = user.FindFirst("oid")?.Value
                      ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value
                      ?? user.FindFirst("sub")?.Value
                      ?? "unknown";

            var correlationId = httpContextAccessor.HttpContext?.Request.Headers["X-Correlation-Id"].FirstOrDefault()
                             ?? Guid.NewGuid().ToString();

            var tenantClaim = user.FindFirst("tenant_id")?.Value;
            Guid? tenantId = Guid.TryParse(tenantClaim, out var tid) ? tid : null;

            var roles = user.FindAll(ClaimTypes.Role)
                           .Select(c => c.Value)
                           .ToList();

            return new RequestContext<string, Guid?>(userId, correlationId, tenantId, roles);
        });
    }

    private static void AddDatabaseServices(IServiceCollection services, IConfiguration config)
    {
        // Interceptors
        services.AddTransient<AuditInterceptor<string, Guid?>>();
        services.AddTransient<ConnectionNoLockInterceptor>();

        var dbConnectionStringTrxn = config.GetConnectionString("TaskFlowDbContextTrxn") ?? "";
        var dbConnectionStringQuery = config.GetConnectionString("TaskFlowDbContextQuery") ?? "";

        // TRXN context: pooled with audit interceptor
        services.AddPooledDbContextFactory<TaskFlowDbContextTrxn>((sp, options) =>
        {
            ConfigureSqlOptions(options, dbConnectionStringTrxn);
            var auditInterceptor = sp.GetRequiredService<AuditInterceptor<string, Guid?>>();
            options.AddInterceptors(auditInterceptor);
        });
        services.AddScoped<DbContextScopedFactory<TaskFlowDbContextTrxn, string, Guid?>>();
        services.AddScoped(sp => sp.GetRequiredService<DbContextScopedFactory<TaskFlowDbContextTrxn, string, Guid?>>()
            .CreateDbContext());

        // QUERY context: pooled, no-tracking, ReadOnly intent
        services.AddPooledDbContextFactory<TaskFlowDbContextQuery>((sp, options) =>
        {
            var readOnlyConnStr = dbConnectionStringQuery.Contains("ApplicationIntent=")
                ? dbConnectionStringQuery
                : dbConnectionStringQuery + ";ApplicationIntent=ReadOnly";
            options.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
            ConfigureSqlOptions(options, readOnlyConnStr);
        });
        services.AddScoped<DbContextScopedFactory<TaskFlowDbContextQuery, string, Guid?>>();
        services.AddScoped(sp => sp.GetRequiredService<DbContextScopedFactory<TaskFlowDbContextQuery, string, Guid?>>()
            .CreateDbContext());

        // Repositories
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

    private static void ConfigureSqlOptions(DbContextOptionsBuilder options, string connectionString)
    {
        if (string.IsNullOrEmpty(connectionString)) return;

        if (connectionString.Contains("database.windows.net"))
        {
            options.UseAzureSql(connectionString, sqlOptions =>
            {
                sqlOptions.UseCompatibilityLevel(170);
                sqlOptions.EnableRetryOnFailure(maxRetryCount: 5,
                    maxRetryDelay: TimeSpan.FromSeconds(30), errorNumbersToAdd: null);
            });
        }
        else
        {
            options.UseSqlServer(connectionString, sqlOptions =>
            {
                sqlOptions.UseCompatibilityLevel(160);
                sqlOptions.EnableRetryOnFailure(maxRetryCount: 5,
                    maxRetryDelay: TimeSpan.FromSeconds(30), errorNumbersToAdd: null);
            });
        }
    }

    private static void AddCachingServices(IServiceCollection services, IConfiguration config)
    {
        List<CacheSettings> cacheSettings = [];
        config.GetSection("CacheSettings").Bind(cacheSettings);

        if (cacheSettings.Count == 0)
        {
            // Default cache when no config
            cacheSettings.Add(new CacheSettings { Name = AppConstants.DEFAULT_CACHE });
        }

        foreach (var settings in cacheSettings)
        {
            var fcBuilder = services.AddFusionCache(settings.Name)
                .WithSystemTextJsonSerializer(new JsonSerializerOptions
                {
                    ReferenceHandler = ReferenceHandler.Preserve
                })
                .WithCacheKeyPrefix($"{settings.Name}:")
                .WithDefaultEntryOptions(new FusionCacheEntryOptions
                {
                    Duration = TimeSpan.FromMinutes(settings.DurationMinutes),
                    DistributedCacheDuration = TimeSpan.FromMinutes(settings.DistributedCacheDurationMinutes),
                    IsFailSafeEnabled = true,
                    FailSafeMaxDuration = TimeSpan.FromMinutes(settings.FailSafeMaxDurationMinutes),
                    FailSafeThrottleDuration = TimeSpan.FromSeconds(settings.FailSafeThrottleDurationSeconds),
                    JitterMaxDuration = TimeSpan.FromSeconds(10),
                    FactorySoftTimeout = TimeSpan.FromSeconds(1),
                    FactoryHardTimeout = TimeSpan.FromSeconds(30),
                    EagerRefreshThreshold = 0.9f
                });

            var redisConnStr = !string.IsNullOrEmpty(settings.RedisConnectionStringName)
                ? config.GetConnectionString(settings.RedisConnectionStringName)
                : null;

            if (!string.IsNullOrEmpty(redisConnStr))
            {
                fcBuilder
                    .WithDistributedCache(new RedisCache(new RedisCacheOptions
                    {
                        Configuration = redisConnStr
                    }))
                    .WithBackplane(new RedisBackplane(new RedisBackplaneOptions
                    {
                        Configuration = redisConnStr
                    }));
            }
        }
    }

    private static void AddHealthChecks(IServiceCollection services)
    {
        services.AddHealthChecks()
            .AddCheck<SqlHealthCheck>("sql", tags: ["ready"]);
    }

    private static void AddApplicationServices(IServiceCollection services)
    {
        // Cross-cutting
        services.AddScoped<ITenantBoundaryValidator, TenantBoundaryValidator>();
        services.AddSingleton<IEntityCacheProvider, NoOpEntityCacheProvider>();

        // Services
        services.AddScoped<ICategoryService, CategoryService>();
        services.AddScoped<ITagService, TagService>();
        services.AddScoped<ITaskItemService, TaskItemService>();
        services.AddScoped<ICommentService, CommentService>();
        services.AddScoped<IChecklistItemService, ChecklistItemService>();
        services.AddScoped<IAttachmentService, AttachmentService>();
        services.AddScoped<ITaskItemTagService, TaskItemTagService>();
    }
}
