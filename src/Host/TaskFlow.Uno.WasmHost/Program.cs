using Microsoft.AspNetCore.Hosting.StaticWebAssets;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Net.Http.Headers;

var builder = WebApplication.CreateBuilder(args);

// Shared Aspire service defaults so this static-asset host reports telemetry (incl. Azure Monitor
// when configured) and health alongside the other .NET hosts. No-ops cleanly without Azure config.
builder.AddServiceDefaults();

var distPath = builder.Configuration["UnoWasm:DistPath"];

if (string.IsNullOrWhiteSpace(distPath))
{
#if DEBUG
    const string configuration = "Debug";
#else
    const string configuration = "Release";
#endif
    distPath = Path.GetFullPath(Path.Combine(
        builder.Environment.ContentRootPath,
        "..",
        "..",
        "UI",
        "TaskFlow.Uno",
        "bin",
        configuration,
        "net10.0-browserwasm"));
}
else
{
    distPath = Path.GetFullPath(distPath);
}

var staticWebAssetsManifestPath = Path.Combine(distPath, "TaskFlow.Uno.staticwebassets.runtime.json");
if (File.Exists(staticWebAssetsManifestPath))
{
    builder.Configuration[WebHostDefaults.StaticWebAssetsKey] = staticWebAssetsManifestPath;
    StaticWebAssetsLoader.UseStaticWebAssets(builder.Environment, builder.Configuration);
}

var app = builder.Build();

app.MapDefaultEndpoints();

var indexPath = Path.Combine(distPath, "wwwroot", "index.html");
if (!File.Exists(indexPath))
{
    app.Logger.LogWarning("Uno WASM assets were not found at {DistPath}. Build TaskFlow.Uno for net10.0-browserwasm first.", distPath);
}

Directory.CreateDirectory(distPath);

var contentTypeProvider = new FileExtensionContentTypeProvider();
contentTypeProvider.Mappings[".dat"] = "application/octet-stream";
contentTypeProvider.Mappings[".pdb"] = "application/octet-stream";

var cacheHeaders = new Action<StaticFileResponseContext>(context =>
{
    context.Context.Response.Headers[HeaderNames.CacheControl] = "no-cache, no-store";
});

app.MapGet("/health", () => Results.Ok(new { status = "Healthy", distPath }));
app.UseDefaultFiles();
app.UseStaticFiles(new StaticFileOptions
{
    ContentTypeProvider = contentTypeProvider,
    OnPrepareResponse = cacheHeaders
});
app.MapFallbackToFile("index.html", new StaticFileOptions
{
    ContentTypeProvider = contentTypeProvider,
    OnPrepareResponse = cacheHeaders
});

app.Run();
