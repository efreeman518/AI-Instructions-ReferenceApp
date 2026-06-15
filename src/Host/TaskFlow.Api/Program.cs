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
startupLogger.LogInformation("{AppName} {Environment} - Startup.", appName, env);

try
{
    // 1. Service defaults (OpenTelemetry, health, resilience)
    builder.AddServiceDefaults();

    // 2. Data Protection (Azure Blob key storage + Key Vault key encryption)
    ConfigureDataProtection();

    // 2b. AI chat client (Aspire-wired Azure AI Foundry / Foundry Local).
    // Connection name "chat" must match the Foundry deployment resource in the AppHost. When the
    // AppHost injected a "chat" reference (CHAT_ENDPOINT / ConnectionStrings:chat), register the
    // Microsoft.Extensions.AI IChatClient here at the host. Otherwise AddAiServices registers a
    // no-op IChatClient so the AI demo endpoints still resolve and the app boots without a model.
    ConfigureChatClient();

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
    startupLogger.LogCritical(ex, "{AppName} {Environment} - Host terminated unexpectedly.", appName, env);
}
finally
{
    startupLogger.LogInformation("{AppName} {Environment} - Ending application.", appName, env);
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

void ConfigureChatClient()
{
    // Aspire injects the deployment connection string as ConnectionStrings:chat when the AppHost
    // wired a Foundry deployment named "chat" (local Foundry Local or real Azure Foundry).
    var chatConnection = config.GetConnectionString("chat");
    if (string.IsNullOrWhiteSpace(chatConnection))
        return;

    startupLogger.LogInformation("{AppName} {Environment} - Configure AI chat client.", appName, env);

    // Registers Azure.AI.Inference ChatCompletionsClient, then a Microsoft.Extensions.AI IChatClient
    // over it. The deployment/model is taken from the connection string.
    builder.AddAzureChatCompletionsClient("chat")
        .AddChatClient();
}

void ConfigureDataProtection()
{
    var keysFileUrl = config.GetValue<string?>("DataProtectionKeysFileUrl", null);
    var encryptionKeyUrl = config.GetValue<string?>("DataProtectionEncryptionKeyUrl", null);
    if (!string.IsNullOrEmpty(keysFileUrl) && !string.IsNullOrEmpty(encryptionKeyUrl))
    {
        startupLogger.LogInformation("{AppName} {Environment} - Configure Data Protection.", appName, env);
        services.AddDataProtection()
            .PersistKeysToAzureBlobStorage(new Uri(keysFileUrl), credential)
            .ProtectKeysWithAzureKeyVault(new Uri(encryptionKeyUrl), credential);
    }
}

// Required for WebApplicationFactory in integration tests
public partial class Program { }
