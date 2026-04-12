using TaskFlow.Gateway;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddGatewayServices(builder.Configuration);

// Authorization (auth registered in AddGatewayServices)
builder.Services.AddAuthorization();

var app = builder.Build();

// Pipeline order: security → CORS → middleware → endpoints → reverse proxy
app.UseExceptionHandler(appBuilder =>
    appBuilder.Run(async ctx =>
    {
        ctx.Response.StatusCode = 500;
        await ctx.Response.WriteAsJsonAsync(new { error = "An unexpected error occurred." });
    }));

app.UseCors("UnoUI");
app.UseAuthentication();
app.UseAuthorization();

app.MapDefaultEndpoints();
app.MapGet("/", () => "TaskFlow Gateway");

app.MapReverseProxy();

app.Run();
