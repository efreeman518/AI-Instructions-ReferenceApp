using TaskFlow.Scheduler.Abstractions;
using TaskFlow.Scheduler.Handlers;
using TickerQ.DependencyInjection;
using TickerQ.Dashboard.DependencyInjection;
using TickerQ.EntityFrameworkCore.DependencyInjection;
using TickerQ.EntityFrameworkCore.DbContextFactory;
using TickerQ.Utilities.Entities;
using TickerQ.Utilities.Interfaces.Managers;
using Microsoft.EntityFrameworkCore;

namespace TaskFlow.Scheduler;

public static class RegisterSchedulerServices
{
    public static IServiceCollection AddSchedulerServices(
        this IServiceCollection services, IConfiguration config)
    {
        // Job handlers
        services.AddScoped<OverdueTaskCheckHandler>();
        services.AddScoped<RecurringTaskGenerationHandler>();
        services.AddScoped<StaleTaskCleanupHandler>();

        return services;
    }

    public static IHostApplicationBuilder AddTickerQConfig(this IHostApplicationBuilder builder)
    {
        var config = builder.Configuration;

        builder.Services.AddTickerQ(options =>
        {
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
                            dbOptions.UseSqlServer(connStr);
                        });
                    });
                }
            }

            var enableDashboard = config.GetValue("Scheduling:EnableDashboard", false);
            if (enableDashboard)
            {
                options.AddDashboard();
            }
        });

        return builder;
    }

    public static async Task SeedCronJobs(this WebApplication app)
    {
        try
        {
            using var scope = app.Services.CreateScope();
            var cronManager = scope.ServiceProvider.GetService<ICronTickerManager<CronTickerEntity>>();
            if (cronManager is null)
            {
                app.Logger.LogWarning("ICronTickerManager not available — cron seeding skipped (in-memory mode)");
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
            app.Logger.LogWarning(ex, "Cron seeding skipped — ICronTickerManager not registered (in-memory mode)");
        }
    }
}
