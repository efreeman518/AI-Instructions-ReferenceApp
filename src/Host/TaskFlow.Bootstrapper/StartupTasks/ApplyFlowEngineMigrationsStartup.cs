using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using TaskFlow.Infrastructure.Data;

namespace TaskFlow.Bootstrapper.StartupTasks;

public sealed class ApplyFlowEngineMigrationsStartup(
    IDbContextFactory<TaskFlowFlowEngineDbContext> factory,
    IConfiguration config,
    ILogger<ApplyFlowEngineMigrationsStartup> logger) : IStartupTask
{
    public async Task ExecuteAsync(CancellationToken ct = default)
    {
        var env = config["ASPNETCORE_ENVIRONMENT"] ?? config["DOTNET_ENVIRONMENT"];
        var isAspire = !string.IsNullOrEmpty(config["DOTNET_ASPIRE"])
            || (config["ASPNETCORE_URLS"]?.Contains("localhost") ?? false);

        if (env != "Development" && !isAspire)
        {
            logger.LogInformation("Skipping FlowEngine auto-migration — not Development or Aspire");
            return;
        }

        try
        {
            await using var db = await factory.CreateDbContextAsync(ct);
            await db.Database.MigrateAsync(ct);
            logger.LogInformation("FlowEngine EF migrations applied successfully");
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "FlowEngine EF migration failed — continuing startup");
        }
    }
}
