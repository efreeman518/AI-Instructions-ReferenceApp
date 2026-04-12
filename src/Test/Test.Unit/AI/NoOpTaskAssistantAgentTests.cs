using Microsoft.Extensions.Logging.Abstractions;
using TaskFlow.Infrastructure.AI.Agents;

namespace Test.Unit.AI;

[TestClass]
[TestCategory("Unit")]
public class NoOpTaskAssistantAgentTests
{
    private readonly NoOpTaskAssistantAgent _agent = new(NullLogger<NoOpTaskAssistantAgent>.Instance);

    [TestMethod]
    public async Task ChatAsync_ReturnsNotConfiguredResponse()
    {
        var request = new AgentChatRequest { Message = "Hello" };

        var response = await _agent.ChatAsync(request, Guid.NewGuid());

        Assert.IsNotNull(response);
        Assert.IsFalse(response.IsConfigured);
        Assert.IsNotNull(response.Message);
        Assert.IsTrue(response.Message.Contains("not configured"));
    }

    [TestMethod]
    public async Task ChatAsync_WithConversationId_PreservesId()
    {
        var conversationId = "conv-123";
        var request = new AgentChatRequest { Message = "Hi", ConversationId = conversationId };

        var response = await _agent.ChatAsync(request, Guid.NewGuid());

        Assert.AreEqual(conversationId, response.ConversationId);
    }

    [TestMethod]
    public async Task ChatAsync_WithoutConversationId_GeneratesId()
    {
        var request = new AgentChatRequest { Message = "Hi" };

        var response = await _agent.ChatAsync(request, Guid.NewGuid());

        Assert.IsNotNull(response.ConversationId);
        Assert.IsTrue(Guid.TryParse(response.ConversationId, out _));
    }
}
