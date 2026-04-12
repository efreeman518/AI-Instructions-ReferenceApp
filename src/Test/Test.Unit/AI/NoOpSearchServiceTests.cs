using Microsoft.Extensions.Logging.Abstractions;
using TaskFlow.Infrastructure.AI.Search;

namespace Test.Unit.AI;

[TestClass]
[TestCategory("Unit")]
public class NoOpSearchServiceTests
{
    private readonly NoOpSearchService _service = new(NullLogger<NoOpSearchService>.Instance);

    [TestMethod]
    public async Task SearchTaskItemsAsync_ReturnsEmptyResults()
    {
        var results = await _service.SearchTaskItemsAsync("test query", SearchMode.Keyword, Guid.NewGuid());

        Assert.IsNotNull(results);
        Assert.AreEqual(0, results.Count);
    }

    [TestMethod]
    public async Task IndexTaskItemAsync_CompletesWithoutError()
    {
        var doc = new TaskItemSearchDocument
        {
            Id = Guid.NewGuid().ToString(),
            TenantId = Guid.NewGuid().ToString(),
            Title = "Test Task",
            Status = "Open",
            Priority = "High",
            LastUpdated = DateTimeOffset.UtcNow
        };

        await _service.IndexTaskItemAsync(doc);
        // No exception = success
    }

    [TestMethod]
    public async Task RemoveTaskItemAsync_CompletesWithoutError()
    {
        await _service.RemoveTaskItemAsync(Guid.NewGuid().ToString());
        // No exception = success
    }
}
