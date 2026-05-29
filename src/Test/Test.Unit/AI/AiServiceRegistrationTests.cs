using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TaskFlow.Infrastructure.AI;
using TaskFlow.Infrastructure.AI.Agents;
using TaskFlow.Infrastructure.AI.Search;

namespace Test.Unit.AI;

/// <summary>
/// Validates <c>AddAiServices</c> DI wiring against an in-memory <see cref="Microsoft.Extensions.Configuration.IConfiguration"/>:
/// without endpoints, the No-Op search and agent implementations are registered; settings bind to
/// <c>TaskFlowAiSettings</c>.
/// Pure-unit tier (in-memory ServiceCollection): no Azure client, no real endpoint.
/// </summary>
[TestClass]
[TestCategory("Unit")]
public class AiServiceRegistrationTests
{
    /// <summary>Verifies add AI services with no config registers no op services behavior and protects the expected test contract.</summary>
    [TestMethod]
    public void AddAiServices_WithNoConfig_RegistersNoOpServices()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AiServices:UseSearch"] = "false",
                ["AiServices:UseAgents"] = "false"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddAiServices(config);

        var provider = services.BuildServiceProvider();

        var searchService = provider.GetRequiredService<ITaskFlowSearchService>();
        var agentService = provider.GetRequiredService<ITaskAssistantAgent>();

        Assert.IsInstanceOfType(searchService, typeof(NoOpSearchService));
        Assert.IsInstanceOfType(agentService, typeof(NoOpTaskAssistantAgent));
    }

    /// <summary>Verifies add AI services with search enabled no endpoint registers no op search behavior and protects the expected test contract.</summary>
    [TestMethod]
    public void AddAiServices_WithSearchEnabled_NoEndpoint_RegistersNoOpSearch()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AiServices:UseSearch"] = "true",
                ["AiServices:SearchEndpoint"] = ""
            })
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddAiServices(config);

        var provider = services.BuildServiceProvider();
        var searchService = provider.GetRequiredService<ITaskFlowSearchService>();

        Assert.IsInstanceOfType(searchService, typeof(NoOpSearchService));
    }

    /// <summary>Verifies add AI services with agents enabled no endpoint registers no op agent behavior and protects the expected test contract.</summary>
    [TestMethod]
    public void AddAiServices_WithAgentsEnabled_NoEndpoint_RegistersNoOpAgent()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AiServices:UseAgents"] = "true",
                ["AiServices:FoundryEndpoint"] = ""
            })
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddAiServices(config);

        var provider = services.BuildServiceProvider();
        var agentService = provider.GetRequiredService<ITaskAssistantAgent>();

        Assert.IsInstanceOfType(agentService, typeof(NoOpTaskAssistantAgent));
    }

    /// <summary>Verifies add AI services settings bind correctly behavior and protects the expected test contract.</summary>
    [TestMethod]
    public void AddAiServices_SettingsBindCorrectly()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AiServices:UseSearch"] = "true",
                ["AiServices:UseAgents"] = "false",
                ["AiServices:SearchIndexName"] = "custom-index",
                ["AiServices:AgentModelDeployment"] = "gpt-4o-custom"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddAiServices(config);

        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<Microsoft.Extensions.Options.IOptions<TaskFlowAiSettings>>();

        Assert.IsTrue(options.Value.UseSearch);
        Assert.IsFalse(options.Value.UseAgents);
        Assert.AreEqual("custom-index", options.Value.SearchIndexName);
        Assert.AreEqual("gpt-4o-custom", options.Value.AgentModelDeployment);
    }
}
