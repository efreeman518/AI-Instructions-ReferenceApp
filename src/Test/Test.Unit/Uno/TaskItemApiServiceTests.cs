using Moq;
using TaskFlow.Uno.Core.Business.Models;
using TaskFlow.Uno.Core.Business.Notifications;
using TaskFlow.Uno.Core.Business.Services;
using TaskFlow.Uno.Core.Client;

namespace Test.Unit.Uno;

[TestClass]
[TestCategory("Unit")]
[TestCategory("Uno")]
public class TaskItemApiServiceTests
{
    private MockHttpMessageHandler _handler = null!;
    private HttpClient _httpClient = null!;
    private TaskFlowApiClient _apiClient = null!;
    private TaskItemApiService _service = null!;

    [TestInitialize]
    public void Setup()
    {
        _handler = new MockHttpMessageHandler();
        _httpClient = new HttpClient(_handler) { BaseAddress = new Uri("https://localhost:7200") };
        _apiClient = new TaskFlowApiClient(_httpClient);
        _service = new TaskItemApiService(_apiClient, Mock.Of<INotificationService>());
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
        Assert.AreEqual("Build dashboard UI", results[0].Title);
        Assert.AreEqual("InProgress", results[0].Status);
        Assert.AreEqual("High", results[0].Priority);
    }

    [TestMethod]
    public async Task SearchAsync_IncludesOverdueTask()
    {
        var results = await _service.SearchAsync();

        var overdueTask = results.FirstOrDefault(t => t.Title == "Fix login validation");
        Assert.IsNotNull(overdueTask);
        Assert.IsTrue(overdueTask.IsOverdue);
    }

    [TestMethod]
    public async Task CreateAsync_ReturnsMappedModel()
    {
        var newTask = new TaskItemModel { Title = "Test task", Priority = "Medium" };

        var result = await _service.CreateAsync(newTask);

        Assert.IsNotNull(result);
        Assert.IsNotNull(result.Id);
    }

    [TestMethod]
    public async Task DeleteAsync_DoesNotThrow()
    {
        await _service.DeleteAsync(Guid.NewGuid());
        // No exception = success
    }
}
