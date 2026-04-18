using TaskFlow.Uno.Core.Business.Models;
using TaskFlow.Uno.Core.Business.Services;
using TaskFlow.Uno.Core.Client;

namespace Test.Unit.Uno;

[TestClass]
[TestCategory("Unit")]
[TestCategory("Uno")]
public class TagApiServiceTests
{
    private MockHttpMessageHandler _handler = null!;
    private HttpClient _httpClient = null!;
    private TaskFlowApiClient _apiClient = null!;
    private TagApiService _service = null!;

    [TestInitialize]
    public void Setup()
    {
        _handler = new MockHttpMessageHandler();
        _httpClient = new HttpClient(_handler) { BaseAddress = new Uri("https://localhost:7200") };
        _apiClient = new TaskFlowApiClient(_httpClient);
        _service = new TagApiService(_apiClient);
    }

    [TestCleanup]
    public void Teardown()
    {
        _httpClient.Dispose();
        _handler.Dispose();
    }

    [TestMethod]
    public async Task SearchAsync_ReturnsMappedModels()
    {
        var results = await _service.SearchAsync();

        Assert.IsNotEmpty(results);
        // Mock SearchTags orders alphabetically — "backend" sorts before "frontend".
        Assert.AreEqual("backend", results[0].Name);
        Assert.AreEqual("#10B981", results[0].Color);
    }

    [TestMethod]
    public async Task GetAsync_ReturnsMappedModel()
    {
        var tagId = Guid.Parse("22222222-2222-2222-2222-111111111111");

        var result = await _service.GetAsync(tagId);

        Assert.IsNotNull(result);
        Assert.AreEqual("frontend", result.Name);
        Assert.AreEqual(tagId, result.Id);
    }

    [TestMethod]
    public async Task CreateAsync_ReturnsMappedModel()
    {
        var newTag = new TagModel { Name = "urgent", Color = "#EF4444" };

        var result = await _service.CreateAsync(newTag);

        Assert.IsNotNull(result);
        Assert.IsNotNull(result.Id);
        Assert.AreEqual("urgent", result.Name);
    }

    [TestMethod]
    public async Task UpdateAsync_ReturnsMappedModel()
    {
        var tag = new TagModel
        {
            Id = Guid.Parse("22222222-2222-2222-2222-111111111111"),
            Name = "updated-tag",
            Color = "#000000"
        };

        var result = await _service.UpdateAsync(tag);

        Assert.IsNotNull(result);
        Assert.IsNotNull(result.Id);
    }

    [TestMethod]
    public async Task DeleteAsync_DoesNotThrow()
    {
        await _service.DeleteAsync(Guid.NewGuid());
    }
}
