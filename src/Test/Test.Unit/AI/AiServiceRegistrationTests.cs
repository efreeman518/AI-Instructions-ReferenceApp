using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TaskFlow.Infrastructure.AI;
using TaskFlow.Infrastructure.AI.Agents;
using TaskFlow.Infrastructure.AI.Search;

namespace Test.Unit.AI;

[TestClass]
[TestCategory("Unit")]
public class AiServiceRegistrationTests
{
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
