var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

var app = builder.Build();

app.MapDefaultEndpoints();
app.MapGet("/", () => "TaskFlow Gateway");

// Phase 5d: YARP reverse proxy will be configured here

app.Run();
