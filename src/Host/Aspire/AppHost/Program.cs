var builder = DistributedApplication.CreateBuilder(args);

// Infrastructure resources
var sqlPassword = builder.AddParameter("sql-password", secret: true);
var sql = builder.AddSqlServer("sql", sqlPassword, port: 38433)
    .WithLifetime(ContainerLifetime.Persistent)
    .WithDataVolume("taskflow-sql-data")
    .WithImageTag("2025-latest");
var taskflowDb = sql.AddDatabase("taskflowdb");

var redis = builder.AddRedis("redis")
    .WithLifetime(ContainerLifetime.Persistent)
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
var cosmos = builder.AddAzureCosmosDB("CosmosDb1")
    .RunAsEmulator();

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
    .WithReference(cosmos)
    // .WithReference(openai)
    // .WithReference(search)
    .WaitFor(sql)
    .WaitFor(redis);

// Scheduler host
builder.AddProject<Projects.TaskFlow_Scheduler>("taskflowscheduler")
    .WithReference(taskflowDb, connectionName: "TaskFlowDbContextTrxn")
    .WithReference(taskflowDb, connectionName: "TaskFlowDbContextQuery")
    .WithReference(redis, connectionName: "Redis1")
    .WithReference(tables)
    .WithReference(serviceBus)
    .WithReplicas(1)
    .WaitFor(sql);

// Gateway host
var gateway = builder.AddProject<Projects.TaskFlow_Gateway>("taskflowgateway")
    .WithReference(api)
    .WaitFor(api);

// Functions host
builder.AddAzureFunctionsProject<Projects.TaskFlow_Functions>("taskflowfunctions")
    .WithHostStorage(storage)
    .WithReference(taskflowDb, connectionName: "TaskFlowDbContextTrxn")
    .WithReference(taskflowDb, connectionName: "TaskFlowDbContextQuery")
    .WithReference(tables)
    .WithReference(blobs)
    .WithReference(serviceBus)
    .WaitFor(sql)
    .WaitFor(storage);

// Uno UI (WASM) — calls Gateway, not API directly
// Uno.Sdk does not expose GetTargetPath; run Uno WASM separately
// builder.AddProject<Projects.TaskFlow_Uno>("taskflowuno")
//     .WithReference(gateway)
//     .WaitFor(gateway);

await builder.Build().RunAsync();

public partial class Program;
