using Azure.AI.OpenAI;
using Azure.Identity;
using Azure.Search.Documents;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TaskFlow.Infrastructure.AI.Agents;
using TaskFlow.Infrastructure.AI.Agents.Tools;
using TaskFlow.Infrastructure.AI.Search;

namespace TaskFlow.Infrastructure.AI;

public static class AiServiceCollectionExtensions
{
    public static IServiceCollection AddAiServices(this IServiceCollection services, IConfiguration config)
    {
        var aiSection = config.GetSection(TaskFlowAiSettings.ConfigSectionName);
        services.AddOptions<TaskFlowAiSettings>()
            .Bind(aiSection)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        var settings = aiSection.Get<TaskFlowAiSettings>() ?? new TaskFlowAiSettings();

        // Azure OpenAI / Foundry Models client — conditional on endpoint config
        if (!string.IsNullOrWhiteSpace(settings.FoundryEndpoint))
        {
            services.AddSingleton(new AzureOpenAIClient(
                new Uri(settings.FoundryEndpoint),
                new DefaultAzureCredential()));
        }

        // Azure AI Search (if configured)
        if (settings.UseSearch)
        {
            if (!string.IsNullOrWhiteSpace(settings.SearchEndpoint))
            {
                services.AddSingleton(new SearchClient(
                    new Uri(settings.SearchEndpoint),
                    settings.SearchIndexName,
                    new DefaultAzureCredential()));

                services.AddScoped<ITaskFlowSearchService, TaskFlowSearchService>();
            }
            else
            {
                // TODO: [CONFIGURE] AI Search endpoint — set AiServices:SearchEndpoint for live search
                services.AddScoped<ITaskFlowSearchService, NoOpSearchService>();
            }
        }
        else
        {
            services.AddScoped<ITaskFlowSearchService, NoOpSearchService>();
        }

        // Agent function tools (always registered — agents and tests both need them)
        services.AddScoped<TaskItemTools>();

        // Agent services
        if (settings.UseAgents)
        {
            if (!string.IsNullOrWhiteSpace(settings.FoundryEndpoint))
            {
                services.AddScoped<ITaskAssistantAgent, TaskAssistantAgentService>();
            }
            else
            {
                // TODO: [CONFIGURE] Foundry endpoint required for live agents
                services.AddScoped<ITaskAssistantAgent, NoOpTaskAssistantAgent>();
            }
        }
        else
        {
            services.AddScoped<ITaskAssistantAgent, NoOpTaskAssistantAgent>();
        }

        return services;
    }
}
