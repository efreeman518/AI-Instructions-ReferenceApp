using System.Text.Json;
using System.Text.Json.Serialization;
using EF.FlowEngine.Dashboard;
using MudBlazor;
using MudBlazor.Services;
using Refit;
using TaskFlow.Blazor.Components;
using TaskFlow.Blazor.Services;

var builder = WebApplication.CreateBuilder(args);

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
    // No auth handler yet — gateway dev mode accepts unauthenticated requests.
    .AddStandardResilienceHandler();

// FlowEngine Dashboard — talks to TaskFlow.Api's MapFlowEngineAdmin via the gateway.
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
