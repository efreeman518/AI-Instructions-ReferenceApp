using TaskFlow.Bootstrapper;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddTaskFlowServices(builder.Configuration);

var app = builder.Build();

app.MapDefaultEndpoints();
app.MapGet("/", () => "TaskFlow Scheduler");

// Phase 5d: Scheduler jobs will be configured here

app.Run();
