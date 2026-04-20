using Moq;
using TaskFlow.Uno.Core.Business.Models;
using TaskFlow.Uno.Core.Business.Notifications;
using TaskFlow.Uno.Core.Business.Services;
using TaskFlow.Uno.Core.Client;

namespace Test.Unit.Uno;

[TestClass]
[TestCategory("Unit")]
[TestCategory("Uno")]
public class CategoryApiServiceTests
{
    private MockHttpMessageHandler _handler = null!;
    private HttpClient _httpClient = null!;
    private TaskFlowApiClient _apiClient = null!;
    private CategoryApiService _service = null!;

    [TestInitialize]
    public void Setup()
    {
        _handler = new MockHttpMessageHandler();
        _httpClient = new HttpClient(_handler) { BaseAddress = new Uri("https://localhost:7200") };
        _apiClient = new TaskFlowApiClient(_httpClient);
        _service = new CategoryApiService(_apiClient, Mock.Of<INotificationService>());
    }

    [TestCleanup]
    public void Teardown()
    {
        _httpClient.Dispose();
        _handler.Dispose();
    }

    [TestMethod]
    public async Task SearchAsync_ReturnsMappedCategories()
    {
        var results = await _service.SearchAsync();

        Assert.IsNotEmpty(results);
        Assert.AreEqual("Development", results[0].Name);
        Assert.IsTrue(results[0].IsActive);
    }

    [TestMethod]
    public async Task CreateAsync_ReturnsMappedModel()
    {
        var newCategory = new CategoryModel { Name = "Test category", IsActive = true };

        var result = await _service.CreateAsync(newCategory);

        Assert.IsNotNull(result);
        Assert.IsNotNull(result.Id);
    }
}
