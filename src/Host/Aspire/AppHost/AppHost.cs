using AppHost;

var builder = DistributedApplication.CreateBuilder(args);

// Testing environment: set by test classes before calling DistributedApplicationTestingBuilder.
// Skip heavy/optional resources to keep startup fast and avoid func.exe dependency.
var isTesting = Environment.GetEnvironmentVariable("TASKFLOW_ASPIRE_TESTING") == "true"
    || string.Equals(builder.Environment.EnvironmentName, "Testing", StringComparison.OrdinalIgnoreCase);
var applicationStyle = Environment.GetEnvironmentVariable("TASKFLOW_APPLICATION_STYLE");

// Keep SQL password stable across restarts so persistent SQL volumes remain usable.
// Tests can still override via Parameters__sql-password.
var defaultSqlPassword = LocalSqlSettings.SharedSaPassword;

// Infrastructure resources
var sqlPassword = builder.AddParameter("sql-password", defaultSqlPassword, secret: true);
// In Testing mode: non-persistent, no named volume, random port — ensures fresh container with known password.
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

// Azure Storage (Blob) — emulator
// Not using ContainerLifetime.Persistent — persistent emulator containers survive Aspire restarts
// but get stranded on deleted Podman networks, causing netavark "eth2 already exists" errors.
var storage = builder.AddAzureStorage("AzureStorage").RunAsEmulator();
var blobs = storage.AddBlobs("BlobStorage1");
var tables = storage.AddTables("TableStorage1");

// Azure Service Bus — emulator
var serviceBus = builder.AddAzureServiceBus("ServiceBus1").RunAsEmulator();
var domainEventsTopic = serviceBus.AddServiceBusTopic("DomainEvents");
domainEventsTopic.AddServiceBusSubscription("function-processor");
serviceBus.AddServiceBusQueue("TaskCommands");

// Azure Cosmos DB — emulator (see AzureStorage comment re: Persistent lifetime)
// Skipped in Testing: the emulator is heavy (~1.3 GB) and not needed for audit pipeline tests.
// The API's AddCosmosDbServices falls back to NoOpTaskViewRepository when the connection string is absent.
if (!isTesting)
{
    builder.AddAzureCosmosDB("CosmosDb1")
        .RunAsEmulator();
}

// AI resources (deployment-only — no emulator available)
// TODO: [CONFIGURE] Uncomment when Azure AI resources are provisioned
// var openai = builder.AddAzureOpenAI("openai")
//     .AddDeployment(new("gpt-4o-deploy", "gpt-4o", "2024-08-06", "GlobalStandard", 10))
//     .AddDeployment(new("embedding-deploy", "text-embedding-3-small", "1", "GlobalStandard", 10));
// var search = builder.AddAzureSearch("search");

// API host
var api = builder.AddProject<Projects.TaskFlow_Api>("taskflowapi")
    .WithReference(taskflowDb, connectionName: "TaskFlowDbContextTrxn")
    .WithReference(taskflowDb, connectionName: "TaskFlowDbContextQuery")
    .WithReference(redis, connectionName: "Redis1")
    .WithReference(tables)
    .WithReference(blobs)
    .WithReference(serviceBus)
    // .WithReference(openai)
    // .WithReference(search)
    .WaitFor(sql)
    .WaitFor(redis)
    .WaitFor(serviceBus);

if (isTesting)
{
    api.WithEnvironment("Cors__AllowedOrigins__0", "http://localhost");
}

if (!string.IsNullOrWhiteSpace(applicationStyle))
{
    api.WithEnvironment("TASKFLOW_APPLICATION_STYLE", applicationStyle);
}

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

    // Gateway host
    var gateway = builder.AddProject<Projects.TaskFlow_Gateway>("taskflowgateway")
        .WithReference(api)
        .WaitFor(api);

    builder.AddViteApp("taskflowreact", "../../../UI/TaskFlow.React")
        .WithReference(gateway)
        .WithEnvironment("VITE_API_BASE_URL", gateway.GetEndpoint("http"))
        .WaitFor(gateway)
        .WithExternalHttpEndpoints();

    builder.AddProject<Projects.TaskFlow_Uno_WasmHost>("taskflowuno")
        .WithReference(gateway)
        .WaitFor(gateway)
        .WithExternalHttpEndpoints();
}

if (!isTesting || Environment.GetEnvironmentVariable("TASKFLOW_INCLUDE_FUNCTIONS") == "true")
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
}

await builder.Build().RunAsync();

public partial class Program;
