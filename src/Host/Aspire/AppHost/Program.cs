var builder = DistributedApplication.CreateBuilder(args);

// Infrastructure resources
var sqlPassword = builder.AddParameter("sql-password", secret: true);
var sql = builder.AddSqlServer("sql", sqlPassword, port: 38433)
    .WithLifetime(ContainerLifetime.Persistent)
    .WithDataVolume("taskflow-sql-data");
var taskflowDb = sql.AddDatabase("taskflowdb");

var redis = builder.AddRedis("redis")
    .WithLifetime(ContainerLifetime.Persistent)
    .WithDataVolume("taskflow-redis-data");

// Azure Storage (Blob) — emulator
var storage = builder.AddAzureStorage("AzureStorage").RunAsEmulator(c => c.WithLifetime(ContainerLifetime.Persistent));
var blobs = storage.AddBlobs("BlobStorage1");

// Azure Service Bus — emulator
var serviceBus = builder.AddAzureServiceBus("ServiceBus1").RunAsEmulator(c => c.WithLifetime(ContainerLifetime.Persistent));
var domainEventsTopic = serviceBus.AddServiceBusTopic("DomainEvents");
domainEventsTopic.AddServiceBusSubscription("function-processor");
serviceBus.AddServiceBusQueue("TaskCommands");

// Azure Cosmos DB — emulator
var cosmos = builder.AddAzureCosmosDB("CosmosDb1")
    .RunAsEmulator(c => c.WithLifetime(ContainerLifetime.Persistent));

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
    .WithReference(serviceBus)
    .WithReplicas(1)
    .WaitFor(sql);

// Gateway host
var gateway = builder.AddProject<Projects.TaskFlow_Gateway>("taskflowgateway")
    .WithReference(api)
    .WaitFor(api);

// Functions host
builder.AddProject<Projects.TaskFlow_Functions>("taskflowfunctions")
    .WithReference(taskflowDb, connectionName: "TaskFlowDbContextTrxn")
    .WithReference(blobs)
    .WithReference(serviceBus)
    .WithEnvironment("AzureWebJobsSecretStorageType", "Files")
    .WithEnvironment(ctx =>
    {
        ctx.EnvironmentVariables["AzureWebJobsStorage"] = storage.Resource;
    })
    .WaitFor(sql)
    .WaitFor(storage);

// Uno UI (WASM) — calls Gateway, not API directly
// Uno.Sdk does not expose GetTargetPath; run Uno WASM separately
// builder.AddProject<Projects.TaskFlow_Uno>("taskflowuno")
//     .WithReference(gateway)
//     .WaitFor(gateway);

await builder.Build().RunAsync();
