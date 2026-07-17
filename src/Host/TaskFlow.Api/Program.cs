using Azure.Identity;
using EF.Common;
using Microsoft.AspNetCore.DataProtection;
using TaskFlow.Api;
using TaskFlow.Bootstrapper;

var builder = WebApplication.CreateBuilder(args);
var config = builder.Configuration;
var services = builder.Services;
var appName = config.GetValue<string>("AppName") ?? "TaskFlow.Api";
var env = config.GetValue<string>("ASPNETCORE_ENVIRONMENT")
    ?? config.GetValue<string>("DOTNET_ENVIRONMENT") ?? "Undefined";
var credential = CreateAzureCredential(config);

ILogger<Program> startupLogger = CreateStartupLogger();
startupLogger.Startup(appName, env);

try
{
    // 1. Service defaults (OpenTelemetry, health, resilience)
    builder.AddServiceDefaults();
    builder.AddProxyForwarding();

    // 2. Data Protection (Azure Blob key storage + Key Vault key encryption)
    ConfigureDataProtection();

    // 2b. AI chat client: shared Azure -> Foundry Local -> no-op strategy.
    await builder.RegisterAiChatClientAsync(startupLogger);

    // 3. Registration chain - order matters for dependency resolution
    services
        .RegisterInfrastructureServices(config)
        .RegisterDomainServices(config)
        .RegisterApplicationServices(config)
        .RegisterBackgroundServices(config)
        .AddApiServices(config, startupLogger);

    // 4. Build + pipeline
    var app = builder.Build().ConfigurePipeline();

    // 5. Startup tasks (migrations, warmup)
    await app.RunStartupTasks();

    // 6. Switch to runtime logger
    StaticLogging.SetStaticLoggerFactory(app.Services.GetRequiredService<ILoggerFactory>());

    await app.RunAsync();
}
catch (Exception ex)
{
    startupLogger.HostTerminated(ex, appName, env);
}
finally
{
    startupLogger.EndingApplication(appName, env);
}

ILogger<Program> CreateStartupLogger()
{
    StaticLogging.CreateStaticLoggerFactory(logBuilder =>
    {
        logBuilder.SetMinimumLevel(LogLevel.Information);
        logBuilder.AddConsole();
    });
    return StaticLogging.CreateLogger<Program>();
}

static DefaultAzureCredential CreateAzureCredential(IConfiguration config)
{
    var options = new DefaultAzureCredentialOptions();
    var managedIdentityClientId = config.GetValue<string?>("ManagedIdentityClientId", null);
    if (managedIdentityClientId is not null)
        options.ManagedIdentityClientId = managedIdentityClientId;
    var sharedTokenCacheTenantId = config.GetValue<string?>("SharedTokenCacheTenantId", null);
    if (sharedTokenCacheTenantId is not null)
        options.SharedTokenCacheTenantId = sharedTokenCacheTenantId;
    return new DefaultAzureCredential(options);
}

void ConfigureDataProtection()
{
    var keysFileUrl = config.GetValue<string?>("DataProtectionKeysFileUrl", null);
    var encryptionKeyUrl = config.GetValue<string?>("DataProtectionEncryptionKeyUrl", null);
    if (!string.IsNullOrEmpty(keysFileUrl) && !string.IsNullOrEmpty(encryptionKeyUrl))
    {
        startupLogger.ConfigureDataProtection(appName, env);
        services.AddDataProtection()
            .PersistKeysToAzureBlobStorage(new Uri(keysFileUrl), credential)
            .ProtectKeysWithAzureKeyVault(new Uri(encryptionKeyUrl), credential);
    }
}

// Required so cross-assembly integration/smoke test projects (Test.Endpoints, Test.FoundryLocal,
// Test.E2E, ...) can reference this host as WebApplicationFactory<Program>. The .NET 10 auto-generated
// Program is internal, so the explicit public declaration is load-bearing here despite ASP0027.
#pragma warning disable ASP0027 // Public partial Program is required for external WebApplicationFactory access.
public partial class Program { }
#pragma warning restore ASP0027
