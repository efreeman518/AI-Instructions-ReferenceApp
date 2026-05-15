using Azure.AI.OpenAI;
using EF.FlowEngine;
using EF.FlowEngine.AdminApi;
using EF.FlowEngine.Clients.Http;
using EF.FlowEngine.Clients.OpenAI;
using EF.FlowEngine.Clients.ServiceBus;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TaskFlow.Infrastructure.AI;
using TaskFlow.Infrastructure.Data;

namespace TaskFlow.Bootstrapper;

public static partial class RegisterServices
{
    // FlowEngine v1.0.104 wiring — engine runtime + connector clients + JSON workflow seeding.
    // The 19 built-in node executors are auto-registered by AddFlowEngine() in this version.
    // Engine state + outbox live in TaskFlowFlowEngineDbContext (separate schema, shared SQL connection).
    // The Dashboard + Designer live in TaskFlow.Blazor and call into MapFlowEngineAdmin via the gateway.
    private static void AddFlowEngineServices(IServiceCollection services, IConfiguration config)
    {
        var fe = services.AddFlowEngine(options =>
        {
            options.DefaultLeaseDuration = TimeSpan.FromSeconds(30);
            options.LeaseRenewalInterval = TimeSpan.FromSeconds(13);
            options.SweepInterval = TimeSpan.FromSeconds(30);
            options.SweepBatchSize = 50;
        })
            .UseStateStoreSql<TaskFlowFlowEngineDbContext>()
            .UseLockProviderSql<TaskFlowFlowEngineDbContext>()
            .UseWorkflowRegistrySql<TaskFlowFlowEngineDbContext>()
            .UseHumanTaskStoreSql<TaskFlowFlowEngineDbContext>()
            .UseOutboxSql<TaskFlowFlowEngineDbContext>()
            .UseCircuitBreakerSql<TaskFlowFlowEngineDbContext>();

        AddTaskFlowConnectorClients(fe, services, config);
        AddWorkflowJsonSeeding(fe);

        services.AddFlowEngineAdminPolicies();
    }

    private static void AddTaskFlowConnectorClients(
        FlowEngineBuilder fe,
        IServiceCollection services,
        IConfiguration config)
    {
        // Self-call client — workflows that mutate TaskItems do so through the public API
        // (preserves auth, validation, audit, integration-event publishing).
        var apiBaseUrl = config["FlowEngine:TaskFlowApiBaseUrl"]
            ?? config["Gateway:BaseUrl"]
            ?? "https://localhost";
        services.AddHttpClient("taskflow-api", c => c.BaseAddress = new Uri(apiBaseUrl));
        fe.AddResilientHttpClient("taskflow-api", namedClient: "taskflow-api");

        // Service Bus message client — uses the same connection string as the application's
        // integration event publisher. Workflow `message` nodes publish through this; the
        // existing FunctionServiceBusTrigger picks them up alongside domain events.
        var sbConnStr = ResolveConnectionString(config, "ServiceBus1", "Values:ServiceBus1");
        if (!string.IsNullOrEmpty(sbConnStr))
        {
            var topic = config["FlowEngine:ServiceBusTopic"] ?? "taskflow-integration-events";
            fe.AddServiceBusClient("integration-events", sbConnStr, topic);
        }

        // Azure OpenAI agent client — v1.0.104 introduces AddAzureOpenAIAgentClient,
        // which takes the AzureOpenAIClient factory + deployment/model names directly
        // instead of an Microsoft.Extensions.AI.IChatClient adapter.
        var foundryEndpoint = config[$"{TaskFlowAiSettings.ConfigSectionName}:FoundryEndpoint"];
        var chatDeployment = config[$"{TaskFlowAiSettings.ConfigSectionName}:ChatDeployment"] ?? "gpt-4o";
        if (!string.IsNullOrWhiteSpace(foundryEndpoint))
        {
            fe.AddAzureOpenAIAgentClient(
                clientRef: "ai-agent",
                azureClientFactory: sp => sp.GetRequiredService<AzureOpenAIClient>(),
                deploymentName: chatDeployment,
                modelName: chatDeployment);
        }
    }

    // JSON workflow definitions live in TaskFlow.Api/Workflows/. The seeding service is a
    // hosted service that runs once at startup, skipping the directory if it does not exist
    // (e.g. when this assembly is loaded by TaskFlow.Functions or TaskFlow.Scheduler).
    // Replaces the bespoke WorkflowSeedStartupTask in pre-1.0.104 versions.
    private static void AddWorkflowJsonSeeding(FlowEngineBuilder fe)
    {
        fe.AddWorkflowJsonSeeding(opts =>
        {
            opts.Directory = "Workflows";
            opts.SearchPattern = "*.json";
            opts.ActivateOnSeed = true;
            opts.OverwriteExistingVersion = false;
        });
    }
}
