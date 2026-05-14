using Azure.AI.OpenAI;
using EF.FlowEngine;
using EF.FlowEngine.AdminApi;
using EF.FlowEngine.Clients.Http;
using EF.FlowEngine.Clients.OpenAI;
using EF.FlowEngine.Clients.ServiceBus;
using EF.FlowEngine.Executors;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TaskFlow.Infrastructure.AI;
using TaskFlow.Infrastructure.Data;

namespace TaskFlow.Bootstrapper;

public static partial class RegisterServices
{
    // FlowEngine wiring — engine runtime + node executors + connector clients.
    // Engine state lives in TaskFlowFlowEngineDbContext (separate schema, shared SQL connection).
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
            .UseOutboxSql<TaskFlowFlowEngineDbContext>();

        AddBuiltInNodeExecutors(fe);
        AddTaskFlowConnectorClients(fe, services, config);

        services.AddFlowEngineAdminPolicies();
    }

    private static void AddBuiltInNodeExecutors(FlowEngineBuilder fe)
    {
        fe.AddNodeExecutor<FilterNodeExecutor>()
          .AddNodeExecutor<DecisionNodeExecutor>()
          .AddNodeExecutor<StoreNodeExecutor>()
          .AddNodeExecutor<ComputeNodeExecutor>()
          .AddNodeExecutor<TransformNodeExecutor>()
          .AddNodeExecutor<IntegrationNodeExecutor>()
          .AddNodeExecutor<FetchNodeExecutor>()
          .AddNodeExecutor<QueryNodeExecutor>()
          .AddNodeExecutor<AgentNodeExecutor>()
          .AddNodeExecutor<MessageNodeExecutor>()
          .AddNodeExecutor<LoopNodeExecutor>()
          .AddNodeExecutor<WorkflowNodeExecutor>()
          .AddNodeExecutor<ParallelNodeExecutor>()
          .AddNodeExecutor<HumanNodeExecutor>()
          .AddNodeExecutor<WaitNodeExecutor>()
          .AddNodeExecutor<TimerNodeExecutor>()
          .AddNodeExecutor<OutputNodeExecutor>()
          .AddNodeExecutor<CheckpointNodeExecutor>()
          .AddNodeExecutor<DocumentNodeExecutor>();
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

        // Agent client — only when Azure OpenAI is configured (matches existing AI service gating).
        var foundryEndpoint = config[$"{TaskFlowAiSettings.ConfigSectionName}:FoundryEndpoint"];
        var chatDeployment = config[$"{TaskFlowAiSettings.ConfigSectionName}:ChatDeployment"] ?? "gpt-4o";
        if (!string.IsNullOrWhiteSpace(foundryEndpoint))
        {
            fe.AddOpenAIAgentClient(
                clientRef: "ai-agent",
                chatClientFactory: sp =>
                {
                    var azureClient = sp.GetRequiredService<AzureOpenAIClient>();
                    return azureClient.GetChatClient(chatDeployment).AsIChatClient();
                },
                modelName: chatDeployment);
        }
    }
}
