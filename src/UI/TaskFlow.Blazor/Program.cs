using System.Text.Json;
using System.Text.Json.Serialization;
using EF.FlowEngine.Dashboard;
using MudBlazor;
using MudBlazor.Services;
using Refit;
using TaskFlow.Blazor.Components;
using TaskFlow.Blazor.Services;

var builder = WebApplication.CreateBuilder(args);

// Blazor Server host for CRUD pages and FlowEngine dashboard pages. API calls go through
// the gateway so auth, claim forwarding, and routing match the other front ends.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddMudServices(config =>
{
    config.SnackbarConfiguration.PositionClass = Defaults.Classes.Position.BottomRight;
    config.SnackbarConfiguration.PreventDuplicates = true;
    config.SnackbarConfiguration.VisibleStateDuration = 4000;
});

builder.Services.AddScoped<FloatService>();

var jsonOptions = new JsonSerializerOptions
{
    PropertyNameCaseInsensitive = true,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    Converters = { new JsonStringEnumConverter() }
};

var gatewayBaseUrl = builder.Configuration["Gateway:BaseUrl"]
    ?? throw new InvalidOperationException("Gateway:BaseUrl not configured.");

builder.Services
    .AddRefitClient<ITaskFlowApiClient>(new RefitSettings
    {
        ContentSerializer = new SystemTextJsonContentSerializer(jsonOptions)
    })
    .ConfigureHttpClient(client =>
    {
        client.BaseAddress = new Uri(gatewayBaseUrl);
        client.DefaultRequestHeaders.Add("Accept", "application/json");
    })
    // No auth handler yet - gateway dev mode accepts unauthenticated requests.
    .AddStandardResilienceHandler();

// Raw HTTP client for the AI demo endpoints (the typed Refit client does not cover the AI routes,
// and the streaming chat demo needs raw Server-Sent Events). Points at the gateway like the others.
builder.Services.AddHttpClient("TaskFlowAi", client =>
{
    client.BaseAddress = new Uri(gatewayBaseUrl);
    client.Timeout = TimeSpan.FromMinutes(2);
});

// FlowEngine Dashboard - talks to TaskFlow.Api's MapFlowEngineAdmin via the gateway.
// Pages contributed by the package are picked up via Routes.razor's AdditionalAssemblies.
var flowEngineAdminBaseUrl = builder.Configuration["FlowEngine:AdminApiBaseUrl"]
    ?? new Uri(new Uri(gatewayBaseUrl), "/api/flowengine/").ToString();
builder.Services.AddFlowEngineDashboard(adminApiBaseUrl: flowEngineAdminBaseUrl);

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
