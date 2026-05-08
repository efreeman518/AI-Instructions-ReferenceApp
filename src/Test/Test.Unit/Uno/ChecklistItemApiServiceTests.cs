using Moq;
using TaskFlow.Uno.Core.Business.Models;
using TaskFlow.Uno.Core.Business.Notifications;
using TaskFlow.Uno.Core.Business.Services;
using TaskFlow.Uno.Core.Client;

namespace Test.Unit.Uno;

/// <summary>
/// Validates <c>ChecklistItemApiService</c> against <c>MockHttpMessageHandler</c>: search returns
/// items with sort order preserved, get/create/update/delete map correctly to the Uno model.
/// Pure-unit tier: in-process <c>HttpClient</c> with mock handler — no real server.
/// </summary>
[TestClass]
[TestCategory("Unit")]
[TestCategory("Uno")]
public class ChecklistItemApiServiceTests
{
    private MockHttpMessageHandler _handler = null!;
    private HttpClient _httpClient = null!;
    private TaskFlowApiClient _apiClient = null!;
    private ChecklistItemApiService _service = null!;

    [TestInitialize]
    public void Setup()
    {
        _handler = new MockHttpMessageHandler();
        _httpClient = new HttpClient(_handler) { BaseAddress = new Uri("https://localhost:7200") };
        _apiClient = new TaskFlowApiClient(_httpClient);
        _service = new ChecklistItemApiService(_apiClient, Mock.Of<INotificationService>());
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
        Assert.HasCount(2, results);
        Assert.AreEqual("Design mockups", results[0].Title);
        Assert.IsTrue(results[0].IsCompleted);
        Assert.AreEqual("Implement XAML", results[1].Title);
        Assert.IsFalse(results[1].IsCompleted);
    }

    [TestMethod]
    public async Task SearchAsync_SortOrderIsPreserved()
    {
        var results = await _service.SearchAsync();

        Assert.AreEqual(1, results[0].SortOrder);
        Assert.AreEqual(2, results[1].SortOrder);
    }

    [TestMethod]
    public async Task GetAsync_ReturnsMappedModel()
    {
        var checklistId = Guid.Parse("55555555-5555-5555-5555-111111111111");

        var result = await _service.GetAsync(checklistId);

        Assert.IsNotNull(result);
        Assert.AreEqual("Design mockups", result.Title);
        Assert.IsTrue(result.IsCompleted);
    }

    [TestMethod]
    public async Task CreateAsync_ReturnsMappedModel()
    {
        var taskId = Guid.Parse("33333333-3333-3333-3333-111111111111");
        var newItem = new ChecklistItemModel { Title = "New checklist item", TaskItemId = taskId };

        var result = await _service.CreateAsync(newItem);

        Assert.IsNotNull(result);
        Assert.IsNotNull(result.Id);
    }

    [TestMethod]
    public async Task UpdateAsync_ReturnsMappedModel()
    {
        var item = new ChecklistItemModel
        {
            Id = Guid.Parse("55555555-5555-5555-5555-111111111111"),
            Title = "Updated item",
            IsCompleted = true,
            SortOrder = 1,
            TaskItemId = Guid.Parse("33333333-3333-3333-3333-111111111111")
        };

        var result = await _service.UpdateAsync(item);

        Assert.IsNotNull(result);
        Assert.IsNotNull(result.Id);
    }

    [TestMethod]
    public async Task DeleteAsync_DoesNotThrow()
    {
        await _service.DeleteAsync(Guid.NewGuid());
    }
}
