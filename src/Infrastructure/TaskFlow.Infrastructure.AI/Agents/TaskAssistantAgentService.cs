using System.Reflection;
using Azure.AI.OpenAI;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenAI.Chat;

namespace TaskFlow.Infrastructure.AI.Agents;

public class TaskAssistantAgentService : ITaskAssistantAgent
{
    private readonly ChatClientAgent _agent;
    private readonly ILogger<TaskAssistantAgentService> _logger;

    // Per-conversation session tracking (scoped per user via DI)
    private AgentSession? _session;

    public TaskAssistantAgentService(
        ILogger<TaskAssistantAgentService> logger,
        AzureOpenAIClient openAiClient,
        IOptions<TaskFlowAiSettings> settings,
        Tools.TaskItemTools tools)
    {
        _logger = logger;

        var systemPrompt = ReadEmbeddedPrompt("TaskAssistant.system-prompt.txt");

        _agent = openAiClient
            .GetChatClient(settings.Value.AgentModelDeployment)
            .AsAIAgent(
                instructions: systemPrompt,
                name: "TaskAssistant",
                tools:
                [
                    AIFunctionFactory.Create(
                        tools.SearchTasks,
                        "SearchTasks",
                        "Search for tasks by keyword, with optional status and priority filters"),
                    AIFunctionFactory.Create(
                        tools.GetTaskDetails,
                        "GetTaskDetails",
                        "Get full details of a specific task by its ID"),
                    AIFunctionFactory.Create(
                        tools.CreateTask,
                        "CreateTask",
                        "Create a new task with a title, optional description, and optional priority"),
                    AIFunctionFactory.Create(
                        tools.UpdateTaskStatus,
                        "UpdateTaskStatus",
                        "Update the status of an existing task (Open, InProgress, Completed, Cancelled, Blocked)"),
                    AIFunctionFactory.Create(
                        tools.SummarizeBacklog,
                        "SummarizeBacklog",
                        "Get a summary of all tasks grouped by status with overdue count")
                ]);
    }

    public async Task<AgentChatResponse> ChatAsync(
        AgentChatRequest request, Guid? tenantId, CancellationToken ct = default)
    {
        _session ??= await _agent.CreateSessionAsync();

        _logger.LogDebug("TaskAssistant processing message for tenant {TenantId}, conversation {ConversationId}",
            tenantId, request.ConversationId);

        var response = await _agent.RunAsync(request.Message, _session, cancellationToken: ct);

        return new AgentChatResponse
        {
            Message = response.ToString(),
            ConversationId = request.ConversationId ?? Guid.NewGuid().ToString(),
            IsConfigured = true
        };
    }

    private static string ReadEmbeddedPrompt(string fileName)
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = assembly.GetManifestResourceNames()
            .FirstOrDefault(n => n.EndsWith(fileName, StringComparison.OrdinalIgnoreCase));

        if (resourceName is null)
            return string.Empty;

        using var stream = assembly.GetManifestResourceStream(resourceName)!;
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
