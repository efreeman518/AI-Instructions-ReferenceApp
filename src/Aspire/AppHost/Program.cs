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

// API host
var api = builder.AddProject<Projects.TaskFlow_Api>("taskflowapi")
    .WithReference(taskflowDb, connectionName: "TaskFlowDbContextTrxn")
    .WithReference(taskflowDb, connectionName: "TaskFlowDbContextQuery")
    .WithReference(redis, connectionName: "Redis1")
    .WaitFor(sql)
    .WaitFor(redis);

// Scheduler host
builder.AddProject<Projects.TaskFlow_Scheduler>("taskflowscheduler")
    .WithReference(taskflowDb, connectionName: "TaskFlowDbContextTrxn")
    .WithReference(taskflowDb, connectionName: "TaskFlowDbContextQuery")
    .WithReference(redis, connectionName: "Redis1")
    .WithReplicas(1)
    .WaitFor(sql);

// Gateway host
builder.AddProject<Projects.TaskFlow_Gateway>("taskflowgateway")
    .WithReference(api)
    .WaitFor(api);

// Functions host
builder.AddProject<Projects.TaskFlow_Functions>("taskflowfunctions")
    .WithReference(taskflowDb, connectionName: "TaskFlowDbContextTrxn")
    .WaitFor(sql);

await builder.Build().RunAsync();
