using System.Text.RegularExpressions;
using TaskFlow.Bootstrapper;
using TaskFlow.Scheduler;
using TickerQ.DependencyInjection;
using TickerQ.EntityFrameworkCore.DbContextFactory;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.Services
    .RegisterInfrastructureServices(builder.Configuration)
    .RegisterApplicationServices(builder.Configuration)
    .AddSchedulerServices(builder.Configuration);
builder.AddTickerQConfig();

var app = builder.Build();

// Ensure TickerQ operational store schema is applied (skip if already present)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetService<TickerQDbContext>();
    if (db is not null)
    {
        var conn = db.Database.GetDbConnection();
        await conn.OpenAsync();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT CASE WHEN EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'ticker' AND TABLE_NAME = 'CronTickers') THEN 1 ELSE 0 END";
        var result = await cmd.ExecuteScalarAsync();
        await conn.CloseAsync();

        if (result is not (int)1)
        {
            var script = db.Database.GenerateCreateScript();
            var batches = Regex.Split(script, @"^\s*GO\s*$", RegexOptions.Multiline | RegexOptions.IgnoreCase)
                .Where(b => !string.IsNullOrWhiteSpace(b));
            foreach (var batch in batches)
            {
                await db.Database.ExecuteSqlRawAsync(batch);
            }
        }
    }
}

app.UseTickerQ();
app.MapDefaultEndpoints();

await app.SeedCronJobs();

await app.RunAsync();
