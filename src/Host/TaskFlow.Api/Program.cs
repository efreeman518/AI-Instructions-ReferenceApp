using Azure.Identity;
using EF.Common;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.AI;
using TaskFlow.Api;
using TaskFlow.Api.Ai;
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

    // 2b. AI chat client: Azure Foundry through Aspire first, Foundry Local SDK fallback second.
    // Otherwise AddAiServices registers a no-op IChatClient so the app boots without a model.
    await ConfigureChatClientAsync();

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

async Task ConfigureChatClientAsync()
{
    // Aspire injects ConnectionStrings:chat when the AppHost wired an Azure Foundry deployment.
    var chatConnection = config.GetConnectionString("chat");
    if (!string.IsNullOrWhiteSpace(chatConnection))
    {
        startupLogger.LogInformation("{AppName} {Environment} - Configure Azure AI Foundry chat client.", appName, env);

        builder.AddAzureChatCompletionsClient("chat")
            .AddChatClient();
        return;
    }

    if (!IsFoundryLocalEnabled(config))
        return;

    startupLogger.LogInformation("{AppName} {Environment} - Configure Foundry Local chat client.", appName, env);

    try
    {
        var chatClient = await FoundryLocalChatClient.CreateAsync(
            config["AiServices:LocalModel"] ?? "qwen2.5-0.5b",
            config["AiServices:LocalWebUrl"] ?? "http://127.0.0.1:52415",
            startupLogger);
        services.AddSingleton<IChatClient>(chatClient);
    }
    catch (Exception ex) when (ex is not OperationCanceledException)
    {
        startupLogger.LogWarning(
            ex,
            "{AppName} {Environment} - Foundry Local unavailable. Falling back to no-op AI client.",
            appName,
            env);
    }

    // ALTERNATIVE (Azure-only, opt-in): instead of raw inference, consume a Foundry project +
    // server-hosted agent. Set AiServices:FoundryProjectEndpoint (or read the Aspire-injected PROJ_URI)
    // and add Azure.AI.Projects + Microsoft.Agents.AI.Foundry. Both results are Microsoft.Agents.AI.AIAgent,
    // so ITaskAssistantAgent can wrap either path - only construction differs:
    //
    //   var project = new AIProjectClient(new Uri(projectEndpoint), CreateAzureCredential(config));
    //   // code-first responses agent (no server-side resource created):
    //   AIAgent agent = project.AsAIAgent(model: deploymentName, name: "TaskAssistant", instructions: prompt);
    //   // or bind to a pre-existing agent created in the portal/IaC, by name:
    //   var record = await project.AgentAdministrationClient.GetAgentAsync(config["AiServices:FoundryAgentName"]);
    //   AIAgent agent = project.AsAIAgent(record);
}

static bool IsFoundryLocalEnabled(IConfiguration config) =>
    config.GetValue<bool>("TASKFLOW_ENABLE_FOUNDRY_LOCAL")
    || config.GetValue<bool>("MYAPP_ENABLE_FOUNDRY_LOCAL");

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
