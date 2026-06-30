using System.Text.RegularExpressions;
using TaskFlow.Infrastructure.Data;
using TaskFlow.Scheduler.Abstractions;
using TaskFlow.Scheduler.Handlers;
using TaskFlow.Scheduler.Infrastructure;
using TaskFlow.Scheduler.Jobs;
using TaskFlow.Scheduler.Telemetry;
using TickerQ.DependencyInjection;
using TickerQ.Dashboard.DependencyInjection;
using TickerQ.EntityFrameworkCore.DependencyInjection;
using TickerQ.EntityFrameworkCore.DbContextFactory;
using TickerQ.Utilities.Entities;
using TickerQ.Utilities.Interfaces.Managers;
using Microsoft.EntityFrameworkCore;

namespace TaskFlow.Scheduler;

/// <summary>
/// Scheduler composition and operational-store setup. TickerQ owns scheduling mechanics;
/// TaskFlow handlers own the domain work invoked by each cron function.
/// </summary>
public static class RegisterSchedulerServices
{
    /// <summary>
    /// Registers job handlers, metrics, and scheduler health checks without configuring the
    /// TickerQ engine itself.
    /// </summary>
    public static IServiceCollection AddSchedulerServices(
        this IServiceCollection services, IConfiguration config)
    {
        // Job handlers
        services.AddScoped<OverdueTaskCheckHandler>();
        services.AddScoped<RecurringTaskGenerationHandler>();
        services.AddScoped<StaleTaskCleanupHandler>();
        services.AddScoped<TaskMaintenanceJobs>();
        services.AddSingleton<SchedulingMetrics>();
        services.AddHealthChecks()
            .AddCheck<SchedulerHealthCheck>("scheduler", tags: ["ready", "memory"]);

        return services;
    }

    /// <summary>
    /// Configures TickerQ execution, optional SQL persistence, and optional dashboard hosting
    /// from Scheduling:* configuration.
    /// </summary>
    public static IHostApplicationBuilder AddTickerQConfig(this IHostApplicationBuilder builder)
    {
        var config = builder.Configuration;
        var maxConcurrency = config.GetValue("Scheduling:MaxConcurrency", Math.Max(1, Environment.ProcessorCount));
        var pollIntervalSeconds = config.GetValue("Scheduling:PollIntervalSeconds", 30);

        builder.Services.AddTickerQ(options =>
        {
            options.SetExceptionHandler<TaskFlowSchedulerExceptionHandler>();

            options.ConfigureScheduler(scheduler =>
            {
                scheduler.MaxConcurrency = maxConcurrency;
                scheduler.SchedulerTimeZone = TimeZoneInfo.Utc;
                scheduler.IdleWorkerTimeOut = TimeSpan.FromMinutes(2);
                scheduler.FallbackIntervalChecker = TimeSpan.FromSeconds(pollIntervalSeconds);
                scheduler.NodeIdentifier = Environment.MachineName;
            });

            var usePersistence = config.GetValue("Scheduling:UsePersistence", true);
            if (usePersistence)
            {
                var connStr = config.GetConnectionString("TaskFlowDbContextTrxn") ?? "";
                if (!string.IsNullOrEmpty(connStr))
                {
                    options.AddOperationalStore(efOptions =>
                    {
                        efOptions.UseTickerQDbContext<TickerQDbContext>(dbOptions =>
                        {
                            dbOptions.UseSqlServer(connStr, sqlOptions =>
                            {
                                sqlOptions.UseLatestCompatibilityLevel();
                                sqlOptions.EnableRetryOnFailure(3, TimeSpan.FromSeconds(10), null);
                                sqlOptions.MigrationsHistoryTable("__EFMigrationsHistory", "Scheduler");
                            });
                        }, schema: "Scheduler");
                    });
                }
            }

            var enableDashboard = config.GetValue("Scheduling:EnableDashboard", false);
            if (enableDashboard)
            {
                options.AddDashboard(dashboard =>
                {
                    dashboard.SetBasePath(config["Scheduling:Dashboard:BasePath"] ?? "/scheduler");

                    var username = config["Scheduling:Dashboard:Username"];
                    var password = config["Scheduling:Dashboard:Password"];
                    if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
                        throw new InvalidOperationException(
                            "TickerQ dashboard requires Scheduling:Dashboard:Username and Scheduling:Dashboard:Password.");

                    dashboard.WithBasicAuth(username, password);
                    dashboard.WithSessionTimeout(config.GetValue("Scheduling:Dashboard:SessionTimeoutMinutes", 60));
                });
            }
        });

        return builder;
    }

    /// <summary>
    /// Validates or creates the Scheduler schema for TickerQ's operational store. This is a
    /// local-startup convenience; production can disable auto-create and run generated scripts.
    /// </summary>
    public static async Task ConfigureTickerQDatabase(this WebApplication app)
    {
        var config = app.Configuration;
        var logger = app.Logger;
        var usePersistence = config.GetValue("Scheduling:UsePersistence", true);
        var connectionString = config.GetConnectionString("TaskFlowDbContextTrxn");
        if (!usePersistence || string.IsNullOrWhiteSpace(connectionString))
        {
            logger.LogInformation("TickerQ running without a persisted operational store.");
            return;
        }

        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetService<TickerQDbContext>();
        if (db is null)
        {
            logger.LogWarning("TickerQ DbContext not registered; operational-store validation skipped.");
            return;
        }

        var canConnect = await db.Database.CanConnectAsync();
        if (!canConnect)
            throw new InvalidOperationException("Cannot connect to the TickerQ operational store database.");

        var schemaExists = await VerifyTickerQSchemaAsync(db, logger);
        var autoCreate = config.GetValue("Scheduling:AutoCreateSchema", true);
        if (!schemaExists && !autoCreate)
        {
            throw new InvalidOperationException(
                "TickerQ schema does not exist. Run a deployment script or set Scheduling:AutoCreateSchema=true for local startup.");
        }

        if (!schemaExists)
        {
            logger.LogInformation("TickerQ schema not found; creating operational-store schema.");
            await ExecuteCreateScriptAsync(db);
        }
        else if (!await VerifyTickerQSchemaCurrentAsync(db, logger))
        {
            if (!autoCreate)
            {
                throw new InvalidOperationException(
                    "TickerQ schema is out of date. Run a deployment script or set Scheduling:AutoCreateSchema=true for local startup.");
            }

            logger.LogInformation("TickerQ schema is out of date; applying operational-store schema updates.");
            await ExecuteUpdateScriptAsync(db);
        }

        if (config.GetValue("Scheduling:GenerateDeploymentScript", false))
            await GenerateDeploymentScriptAsync(db, logger);
    }

    /// <summary>
    /// Seeds default cron definitions. In-memory mode may not expose ICronTickerManager, so this
    /// method logs and exits rather than failing scheduler startup.
    /// </summary>
    public static async Task SeedCronJobs(this WebApplication app)
    {
        try
        {
            using var scope = app.Services.CreateScope();
            var cronManager = scope.ServiceProvider.GetService<ICronTickerManager<CronTickerEntity>>();
            if (cronManager is null)
            {
                app.Logger.LogWarning("ICronTickerManager not available - cron seeding skipped (in-memory mode)");
                return;
            }

            // OverdueTaskCheck: every 6 hours
            await cronManager.AddAsync(new CronTickerEntity
            {
                Function = "OverdueTaskCheck",
                Expression = "0 0 */6 * * *"
            });

            // RecurringTaskGeneration: daily at 2 AM
            await cronManager.AddAsync(new CronTickerEntity
            {
                Function = "RecurringTaskGeneration",
                Expression = "0 0 2 * * *"
            });

            // StaleTaskCleanup: weekly on Sunday at 3 AM
            await cronManager.AddAsync(new CronTickerEntity
            {
                Function = "StaleTaskCleanup",
                Expression = "0 0 3 * * 0"
            });

            app.Logger.LogInformation("TickerQ cron jobs seeded successfully");
        }
        catch (InvalidOperationException ex)
        {
            app.Logger.LogWarning(ex, "Cron seeding skipped - ICronTickerManager not registered (in-memory mode)");
        }
    }

    /// <summary>Verifies ticker q schema before startup continues.</summary>
    private static async Task<bool> VerifyTickerQSchemaAsync(TickerQDbContext db, ILogger logger)
    {
        try
        {
            await db.Database.ExecuteSqlRawAsync("SELECT TOP 1 1 FROM [Scheduler].[TimeTickers]");
            return true;
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "TickerQ schema verification failed.");
            return false;
        }
    }

    /// <summary>Verifies ticker q schema matches the currently referenced package version.</summary>
    private static async Task<bool> VerifyTickerQSchemaCurrentAsync(TickerQDbContext db, ILogger logger)
    {
        try
        {
            await db.Database.ExecuteSqlRawAsync("SELECT TOP 1 [IsSystemPaused] FROM [Scheduler].[CronTickers]");
            return true;
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "TickerQ schema is missing one or more required columns.");
            return false;
        }
    }

    /// <summary>Provides the execute create script operation for register scheduler services.</summary>
    private static async Task ExecuteCreateScriptAsync(TickerQDbContext db)
    {
        var script = db.Database.GenerateCreateScript();
        var batches = Regex.Split(script, @"^\s*GO\s*$", RegexOptions.Multiline | RegexOptions.IgnoreCase)
            .Where(batch => !string.IsNullOrWhiteSpace(batch));

        foreach (var batch in batches)
        {
            await db.Database.ExecuteSqlRawAsync(batch);
        }
    }

    /// <summary>Applies additive updates needed by local TickerQ operational-store schemas.</summary>
    private static async Task ExecuteUpdateScriptAsync(TickerQDbContext db)
    {
        await db.Database.ExecuteSqlRawAsync("""
            IF NOT EXISTS (
                SELECT 1
                FROM sys.columns
                WHERE object_id = OBJECT_ID(N'[Scheduler].[CronTickers]')
                    AND name = N'IsSystemPaused')
            BEGIN
                ALTER TABLE [Scheduler].[CronTickers]
                    ADD [IsSystemPaused] bit NOT NULL
                        CONSTRAINT [DF_CronTickers_IsSystemPaused] DEFAULT CAST(0 AS bit)
            END
            """);
    }

    /// <summary>Generates deployment script from current configuration.</summary>
    private static async Task GenerateDeploymentScriptAsync(TickerQDbContext db, ILogger logger)
    {
        var scriptPath = Path.Combine(AppContext.BaseDirectory, "TickerQ_Deployment.sql");
        var script = $"""
            -- TickerQ Database Deployment Script
            -- Generated: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC
            -- Schema: [Scheduler]

            {db.Database.GenerateCreateScript()}
            """;

        await File.WriteAllTextAsync(scriptPath, script);
        logger.LogInformation("TickerQ deployment script generated at {ScriptPath}", scriptPath);
    }
}
