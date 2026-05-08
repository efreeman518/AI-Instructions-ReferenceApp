using TaskFlow.Uno.Core.Business.Models;
using TaskFlow.Uno.Core.Business.Services;
using TaskFlow.Uno.Core.Client;

namespace Test.Unit.Uno;

/// <summary>
/// Validates the <c>MockHttpMessageHandler</c> used by the Uno design-mode client: routes return the
/// expected mock payloads (TaskItems, Categories, Tags), DELETE returns 204, and unknown routes return 404.
/// Pure-unit tier: the handler is run inside an <c>HttpClient</c> with no real network — verifies the
/// mock surface alone, not API behavior (which Test.Endpoints and Test.E2E cover).
/// </summary>
[TestClass]
[TestCategory("Unit")]
[TestCategory("Uno")]
public class MockHttpMessageHandlerTests
{
    private MockHttpMessageHandler _handler = null!;
    private HttpClient _httpClient = null!;

    [TestInitialize]
    public void Setup()
    {
        _handler = new MockHttpMessageHandler();
        _httpClient = new HttpClient(_handler) { BaseAddress = new Uri("https://localhost:7200") };
    }

    [TestCleanup]
    public void Teardown()
    {
        _httpClient.Dispose();
        _handler.Dispose();
    }

    [TestMethod]
    public async Task SearchTaskItems_ReturnsMockData()
    {
        var response = await _httpClient.PostAsJsonAsync("/api/task-items/search",
            new SearchRequest<TaskItemSearchFilter> { PageNumber = 1, PageSize = 50 });

        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<PagedResponse<TaskItemDto>>();

        Assert.IsNotNull(result);
        Assert.IsNotEmpty(result.Items!);
        Assert.AreEqual("Build dashboard UI", result.Items![0].Title);
    }

    [TestMethod]
    public async Task SearchCategories_ReturnsMockData()
    {
        var response = await _httpClient.PostAsJsonAsync("/api/categories/search",
            new SearchRequest<CategorySearchFilter> { PageNumber = 1, PageSize = 100 });

        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<PagedResponse<CategoryDto>>();

        Assert.IsNotNull(result);
        Assert.IsNotEmpty(result.Items!);
        Assert.AreEqual("Development", result.Items![0].Name);
    }

    [TestMethod]
    public async Task SearchTags_ReturnsMockData()
    {
        var response = await _httpClient.PostAsJsonAsync("/api/tags/search",
            new SearchRequest<TagSearchFilter> { PageNumber = 1, PageSize = 100 });

        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<PagedResponse<TagDto>>();

        Assert.IsNotNull(result);
        Assert.IsNotEmpty(result.Items!);
        // Mock SearchTags orders alphabetically — "backend" sorts before "frontend".
        Assert.AreEqual("backend", result.Items![0].Name);
    }

    [TestMethod]
    public async Task DeleteTaskItem_ReturnsNoContent()
    {
        var response = await _httpClient.DeleteAsync($"/api/task-items/{Guid.NewGuid()}");

        Assert.AreEqual(System.Net.HttpStatusCode.NoContent, response.StatusCode);
    }

    [TestMethod]
    public async Task UnknownRoute_ReturnsNotFound()
    {
        var response = await _httpClient.GetAsync("/api/unknown-endpoint");

        Assert.AreEqual(System.Net.HttpStatusCode.NotFound, response.StatusCode);
    }
}
