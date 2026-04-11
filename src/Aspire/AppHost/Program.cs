var builder = DistributedApplication.CreateBuilder(args);

// Infrastructure resources
var sql = builder.AddSqlServer("sql")
    .AddDatabase("taskflowdb");

var redis = builder.AddRedis("redis");

// API host
var api = builder.AddProject<Projects.TaskFlow_Api>("api")
    .WithReference(sql)
    .WithReference(redis)
    .WaitFor(sql)
    .WaitFor(redis);

// Scheduler host
builder.AddProject<Projects.TaskFlow_Scheduler>("scheduler")
    .WithReference(sql)
    .WithReference(redis)
    .WaitFor(sql);

// Gateway host
builder.AddProject<Projects.TaskFlow_Gateway>("gateway")
    .WithReference(api);

// Functions host
builder.AddProject<Projects.TaskFlow_Functions>("functions")
    .WithReference(sql)
    .WaitFor(sql);

builder.Build().Run();
