using TaskFlow.Bootstrapper;

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddTaskFlowServices();

// Phase 5d: Function triggers and handlers will be configured here

var host = builder.Build();
host.Run();
