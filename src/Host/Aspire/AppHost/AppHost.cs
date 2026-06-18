using System.Linq;
using AppHost;
using Aspire.Hosting.Foundry;

var builder = DistributedApplication.CreateBuilder(args);

// Testing environment: set by test classes before calling DistributedApplicationTestingBuilder.
// Keep test runs isolated and trim only resources that are too heavy or need external tools.
var isTesting = Environment.GetEnvironmentVariable("TASKFLOW_ASPIRE_TESTING") == "true"
    || string.Equals(builder.Environment.EnvironmentName, "Testing", StringComparison.OrdinalIgnoreCase);
var applicationStyle = Environment.GetEnvironmentVariable("TASKFLOW_APPLICATION_STYLE");
var functionsAvailableInTesting =
    Environment.GetEnvironmentVariable("TASKFLOW_ASPIRE_FUNCTIONS_AVAILABLE") == "true";
var reactAvailableInTesting =
    Environment.GetEnvironmentVariable("TASKFLOW_ASPIRE_REACT_AVAILABLE") == "true";
var unoWasmAvailableInTesting =
    Environment.GetEnvironmentVariable("TASKFLOW_ASPIRE_UNO_WASM_AVAILABLE") == "true";

// Keep SQL password stable across restarts so persistent SQL volumes remain usable.
// Tests can still override via Parameters__sql-password.
var defaultSqlPassword = LocalSqlSettings.SharedSaPassword;

// Infrastructure resources
var sqlPassword = builder.AddParameter("sql-password", defaultSqlPassword, secret: true);
// In Testing mode: non-persistent, no named volume, random port - ensures fresh container with known password.
// In dev/prod: persistent with named volume on fixed port.
var sql = builder.AddSqlServer("sql", sqlPassword, port: isTesting ? null : 38433)
    .WithImageTag("2025-latest");
if (!isTesting)
    sql = sql.WithLifetime(ContainerLifetime.Persistent)
             .WithDataVolume("taskflow-sql-data");
var taskflowDb = sql.AddDatabase("taskflowdb");

var redis = builder.AddRedis("redis")
    .WithImageTag("latest");
if (!isTesting)
    redis = redis.WithLifetime(ContainerLifetime.Persistent)
                 .WithDataVolume("taskflow-redis-data");

// Azure Storage (Blob) - emulator
// Not using ContainerLifetime.Persistent - persistent emulator containers survive Aspire restarts
// but get stranded on deleted Podman networks, causing netavark "eth2 already exists" errors.
var storage = builder.AddAzureStorage("AzureStorage")
    .RunAsEmulator(emulator => emulator.WithImageTag("latest"));
var blobs = storage.AddBlobs("BlobStorage1");
var tables = storage.AddTables("TableStorage1");

// Azure Service Bus - emulator
var serviceBus = builder.AddAzureServiceBus("ServiceBus1")
    .RunAsEmulator(emulator => emulator.WithImageTag("latest"));
var domainEventsTopic = serviceBus.AddServiceBusTopic("DomainEvents");
domainEventsTopic.AddServiceBusSubscription("function-processor");
serviceBus.AddServiceBusQueue("TaskCommands");

// The Service Bus emulator bundles its own SQL Server sidecar (ServiceBus1-mssql); the Aspire package
// hardcodes that image to an older tag, and RunAsEmulator's callback cannot reach it. Override it here so
// it matches the `sql` container's 2025-latest tag - keeps the image set on latest and lets Docker share
// layers instead of pulling a second SQL Server major version (cuts CI disk usage).
builder.CreateResourceBuilder(
        (ContainerResource)builder.Resources.Single(r => r.Name == "ServiceBus1-mssql"))
    .WithImageTag("2025-latest");

// Azure Cosmos DB - emulator (see AzureStorage comment re: Persistent lifetime)
// Skipped in Testing: the emulator is heavy (~1.3 GB) and not needed for audit pipeline tests.
// The API's AddCosmosDbServices falls back to NoOpTaskViewRepository when the connection string is absent.
if (!isTesting)
{
    builder.AddAzureCosmosDB("CosmosDb1")
        .RunAsEmulator();
}

// AI: Azure AI Foundry. Two independent axes - lifecycle x consumption.
//
// Axis 1 - lifecycle (where the Foundry resource comes from):
//  - Foundry Local       -> RunAsFoundryLocal(), runs a model on-device (no Azure subscription).
//  - Provision new       -> AddFoundry(...).AddDeployment(...), Bicep creates account + model on publish
//                           (and in run mode when Azure provisioning secrets are set).
//  - Connect to existing -> RunAsExisting/PublishAsExisting against an already-provisioned account
//                           (see the commented block below). Deployment name must already exist there.
//  - Disabled            -> no "chat" resource; the API registers a no-op IChatClient and still boots.
//
// Axis 2 - consumption: this app consumes raw model inference (IChatClient over the "chat" deployment;
// the resource name is the connection name consumers bind to, CHAT_ENDPOINT/etc.). Foundry projects +
// server-hosted agents (AddProject/AddPromptAgent, or pre-existing agents via the client SDK) are an
// Azure-only escalation - see the commented "Foundry project + prompt agent" block after the API host
// and README "AI Demos" -> "Projects and agents". They are documented but not wired by default.
//
// Test mode only wires a model when the test harness found real Azure config or Foundry Local.
IResourceBuilder<FoundryDeploymentResource>? chat = null;
var azureFoundryConfigured = builder.ExecutionContext.IsPublishMode
    || !string.IsNullOrWhiteSpace(builder.Configuration["AiServices:FoundryEndpoint"])
    || Environment.GetEnvironmentVariable("TASKFLOW_USE_AZURE_FOUNDRY") == "true";
var foundryLocalEnabled =
    Environment.GetEnvironmentVariable("TASKFLOW_ENABLE_FOUNDRY_LOCAL") == "true";

if (azureFoundryConfigured)
{
    // Provisions an Azure AI Foundry account + deployment on publish; connects to it in run mode
    // when Azure provisioning is configured (azd / user secrets).
    var foundry = builder.AddFoundry("foundry");
    chat = foundry.AddDeployment("chat", FoundryModel.OpenAI.Gpt4oMini);

    // OPT-IN: connect to an EXISTING Azure Foundry account instead of provisioning a new one.
    // The "chat" deployment must already exist in that account. RunAsExisting binds in run mode;
    // PublishAsExisting binds the published graph. Parameters resolve from config/user-secrets
    // (Parameters:foundry-name / Parameters:foundry-rg). Uncomment and set AiServices:FoundryResourceName
    // + AiServices:FoundryResourceGroup to use it.
    // var foundryName = builder.AddParameter("foundry-name");
    // var foundryRg = builder.AddParameter("foundry-rg");
    // chat = builder.AddFoundry("foundry").RunAsExisting(foundryName, foundryRg)
    //     .AddDeployment("chat", FoundryModel.OpenAI.Gpt4oMini);
}
else if (foundryLocalEnabled)
{
    // Runs the model locally via Foundry Local (requires the Foundry Local runtime installed:
    // `winget install Microsoft.FoundryLocal`). No Azure subscription required. Use a
    // tool-capable model so ChatClientAgent demos can exercise function calling on-device.
    var foundry = builder.AddFoundry("foundry").RunAsFoundryLocal();
    chat = foundry.AddDeployment("chat", FoundryModel.Local.Qwen2505b);
}

// API host
var api = builder.AddProject<Projects.TaskFlow_Api>("taskflowapi")
    .WithReference(taskflowDb, connectionName: "TaskFlowDbContextTrxn")
    .WithReference(taskflowDb, connectionName: "TaskFlowDbContextQuery")
    .WithReference(redis, connectionName: "Redis1")
    .WithReference(tables)
    .WithReference(blobs)
    .WithReference(serviceBus)
    .WaitFor(sql)
    .WaitFor(redis)
    .WaitFor(serviceBus);

// Wire the Foundry chat model into the API when a deployment was created (injects CHAT_ENDPOINT,
// CHAT_APIKEY, CHAT_DEPLOYMENT). Absent -> the API falls back to a no-op IChatClient.
if (chat is not null)
{
    api = api.WithReference(chat);
}

// OPT-IN (Azure-only): Foundry project + server-hosted prompt agent.
// A project is the container for server-hosted agents, deployments, and tool connections. A prompt
// agent is a declarative agent (model + instructions + tools). Prompt agents ALWAYS deploy to Azure
// Foundry, even under `aspire run` - there is no offline path - so this stays commented by default.
// Referencing the project injects PROJ_URI (the project endpoint) into the API; consume pre-existing
// agents at runtime with AIProjectClient.AsAIAgent(...) (see TaskFlow.Api Program.cs ConfigureChatClient).
//
// var foundry = builder.AddFoundry("foundry");
// var project = foundry.AddProject("taskflow-project");
// var projectChat = project.AddModelDeployment("chat", FoundryModel.OpenAI.Gpt41);
// var codeInterp = project.AddCodeInterpreterTool("code-interp");
// var webSearch = project.AddWebSearchTool("web-search");
// var assistant = project.AddPromptAgent(projectChat, "task-assistant",
//         instructions: "You are an assistant for TaskFlow.")
//     .WithTool(codeInterp)
//     .WithTool(webSearch);
// api = api.WithReference(project);   // or .WithReference(assistant)

if (isTesting)
{
    api.WithEnvironment("Cors__AllowedOrigins__0", "http://localhost");
}

if (!string.IsNullOrWhiteSpace(applicationStyle))
{
    api.WithEnvironment("TASKFLOW_APPLICATION_STYLE", applicationStyle);
}

// Gateway is part of the default test graph because all browser-facing hosts route through it.
var gateway = builder.AddProject<Projects.TaskFlow_Gateway>("taskflowgateway")
    .WithReference(api)
    .WithEnvironment("ReverseProxy__Routes__api-route__Match__Path", "/api/{**catch-all}")
    .WithEnvironment("ReverseProxy__Clusters__api-cluster__Destinations__api__Address", api.GetEndpoint("http"))
    .WaitFor(api);

builder.AddProject<Projects.TaskFlow_Blazor>("taskflowblazor")
    .WithReference(gateway)
    .WithEnvironment("Gateway__BaseUrl", gateway.GetEndpoint("http"))
    .WaitFor(gateway)
    .WithExternalHttpEndpoints();

if (!isTesting)
{
    // Scheduler host
    var scheduler = builder.AddProject<Projects.TaskFlow_Scheduler>("taskflowscheduler")
        .WithReference(taskflowDb, connectionName: "TaskFlowDbContextTrxn")
        .WithReference(taskflowDb, connectionName: "TaskFlowDbContextQuery")
        .WithReference(redis, connectionName: "Redis1")
        .WithReference(tables)
        .WithReference(serviceBus)
        .WithReplicas(1)
        .WaitFor(sql)
        .WaitFor(serviceBus);

    if (!string.IsNullOrWhiteSpace(applicationStyle))
    {
        scheduler.WithEnvironment("TASKFLOW_APPLICATION_STYLE", applicationStyle);
    }
}

if (!isTesting || reactAvailableInTesting)
{
    builder.AddViteApp("taskflowreact", "../../../UI/TaskFlow.React")
        .WithReference(gateway)
        .WithEnvironment("VITE_API_BASE_URL", gateway.GetEndpoint("http"))
        .WaitFor(gateway)
        .WithExternalHttpEndpoints();
}

if (!isTesting || unoWasmAvailableInTesting)
{
    builder.AddProject<Projects.TaskFlow_Uno_WasmHost>("taskflowuno")
        .WithReference(gateway)
        .WaitFor(gateway)
        .WithExternalHttpEndpoints();
}

if (!isTesting || functionsAvailableInTesting)
{
    // Functions host
    var functions = builder.AddAzureFunctionsProject<Projects.TaskFlow_Functions>("taskflowfunctions")
        .WithHostStorage(storage)
        .WithReference(taskflowDb, connectionName: "TaskFlowDbContextTrxn")
        .WithReference(taskflowDb, connectionName: "TaskFlowDbContextQuery")
        .WithReference(tables)
        .WithReference(blobs)
        .WithReference(serviceBus)
        .WaitFor(sql)
        .WaitFor(storage)
        .WaitFor(serviceBus);

    if (!string.IsNullOrWhiteSpace(applicationStyle))
    {
        functions.WithEnvironment("TASKFLOW_APPLICATION_STYLE", applicationStyle);
    }

    // Wire the Foundry chat model into Functions for the event-driven AI readiness review (D6).
    if (chat is not null)
    {
        functions.WithReference(chat);
    }
}

await builder.Build().RunAsync();

/// <summary>Configures program host behavior for TaskFlow runtime services.</summary>
public partial class Program;
