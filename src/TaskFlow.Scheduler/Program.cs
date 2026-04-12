using TaskFlow.Bootstrapper;
using TaskFlow.Scheduler;
using TickerQ.DependencyInjection;
using TickerQ.EntityFrameworkCore.DbContextFactory;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddTaskFlowServices(builder.Configuration);
builder.Services.AddSchedulerServices(builder.Configuration);
builder.AddTickerQConfig();

var app = builder.Build();

// Ensure TickerQ operational store schema is applied
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetService<TickerQDbContext>();
    if (db is not null)
    {
        await db.Database.MigrateAsync();
    }
}

app.UseTickerQ();
app.MapDefaultEndpoints();

await app.SeedCronJobs();

await app.RunAsync();
