using TaskFlow.Api.Endpoints;
using TaskFlow.Bootstrapper;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddTaskFlowServices();

var app = builder.Build();

app.MapDefaultEndpoints();

// API endpoint groups
app.MapCategoryEndpoints();
app.MapTagEndpoints();
app.MapTaskItemEndpoints();
app.MapCommentEndpoints();
app.MapChecklistItemEndpoints();
app.MapAttachmentEndpoints();
app.MapTaskItemTagEndpoints();

app.Run();
