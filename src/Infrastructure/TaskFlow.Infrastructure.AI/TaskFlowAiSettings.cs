using System.ComponentModel.DataAnnotations;

namespace TaskFlow.Infrastructure.AI;

public class TaskFlowAiSettings
{
    public const string ConfigSectionName = "AiServices";

    public bool UseSearch { get; set; }
    public bool UseAgents { get; set; }
    public bool UseVectorSearch { get; set; }

    // TODO: [CONFIGURE] Set to your Azure AI Foundry endpoint
    public string? FoundryEndpoint { get; set; }

    // TODO: [CONFIGURE] Model deployment names in your Foundry project
    public string AgentModelDeployment { get; set; } = "gpt-4o-deploy";
    public string EmbeddingModelDeployment { get; set; } = "embedding-deploy";

    // TODO: [CONFIGURE] Set to your Azure AI Search endpoint
    public string? SearchEndpoint { get; set; }
    public string SearchIndexName { get; set; } = "taskitems-index";
}
