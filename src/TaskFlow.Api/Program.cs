using TaskFlow.Bootstrapper;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddTaskFlowServices();

var app = builder.Build();

app.MapDefaultEndpoints();
app.MapGet("/", () => "TaskFlow API");

// Phase 5b: Endpoint groups will be mapped here
// app.MapCategoryEndpoints();
// app.MapTagEndpoints();
// app.MapTaskItemEndpoints();
// app.MapCommentEndpoints();
// app.MapChecklistItemEndpoints();
// app.MapAttachmentEndpoints();

app.Run();
