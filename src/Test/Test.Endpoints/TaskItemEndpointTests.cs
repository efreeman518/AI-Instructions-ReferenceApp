using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using EF.Common.Contracts;
using TaskFlow.Application.Models;
using TaskFlow.Domain.Shared.Enums;

namespace Test.Endpoints;

[TestClass]
public class TaskItemEndpointTests
{
    private static CustomApiFactory _factory = null!;

    [ClassInitialize]
    public static void ClassInit(TestContext _) => _factory = new CustomApiFactory();

    [ClassCleanup]
    public static void ClassCleanup() => _factory?.Dispose();

    private HttpClient CreateClient() => _factory.CreateClient();

    [TestCategory("Endpoint")]
    [TestMethod]
    public async Task Given_ValidPayload_When_PostTaskItem_Then_Returns201()
    {
        using var client = CreateClient();
        var dto = new TaskItemDto { Title = "Test Task", Priority = Priority.Medium };

        var response = await client.PostAsJsonAsync("/api/task-items", dto);

        Assert.AreEqual(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<TaskItemDto>();
        Assert.IsNotNull(created);
        Assert.AreEqual("Test Task", created.Title);
        Assert.IsNotNull(created.Id);
    }

    [TestCategory("Endpoint")]
    [TestMethod]
    public async Task Given_ExistingTaskItem_When_GetById_Then_Returns200()
    {
        using var client = CreateClient();
        var dto = new TaskItemDto { Title = "GetTest", Priority = Priority.Low };
        var createResponse = await client.PostAsJsonAsync("/api/task-items", dto);
        var created = await createResponse.Content.ReadFromJsonAsync<TaskItemDto>();

        var response = await client.GetAsync($"/api/task-items/{created!.Id}");

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<TaskItemDto>();
        Assert.IsNotNull(result);
        Assert.AreEqual("GetTest", result.Title);
    }

    [TestCategory("Endpoint")]
    [TestMethod]
    public async Task Given_NonExistentId_When_GetTaskItem_Then_Returns404()
    {
        using var client = CreateClient();

        var response = await client.GetAsync($"/api/task-items/{Guid.NewGuid()}");

        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
    }

    [TestCategory("Endpoint")]
    [TestMethod]
    public async Task Given_ExistingTaskItem_When_PutUpdate_Then_Returns200()
    {
        using var client = CreateClient();
        var dto = new TaskItemDto { Title = "Before Update", Priority = Priority.Medium };
        var createResponse = await client.PostAsJsonAsync("/api/task-items", dto);
        var created = await createResponse.Content.ReadFromJsonAsync<TaskItemDto>();

        var updateDto = new TaskItemDto
        {
            Id = created!.Id,
            Title = "After Update",
            Priority = Priority.High,
            Status = created.Status
        };
        var response = await client.PutAsJsonAsync($"/api/task-items/{created.Id}", updateDto);

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        var updated = await response.Content.ReadFromJsonAsync<TaskItemDto>();
        Assert.AreEqual("After Update", updated!.Title);
    }

    [TestCategory("Endpoint")]
    [TestMethod]
    public async Task Given_ExistingTaskItem_When_Delete_Then_Returns204()
    {
        using var client = CreateClient();
        var dto = new TaskItemDto { Title = "ToDelete", Priority = Priority.Low };
        var createResponse = await client.PostAsJsonAsync("/api/task-items", dto);
        var created = await createResponse.Content.ReadFromJsonAsync<TaskItemDto>();

        var response = await client.DeleteAsync($"/api/task-items/{created!.Id}");

        Assert.AreEqual(HttpStatusCode.NoContent, response.StatusCode);

        // Verify deleted
        var getResponse = await client.GetAsync($"/api/task-items/{created.Id}");
        Assert.AreEqual(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    [TestCategory("Endpoint")]
    [TestMethod]
    public async Task Given_ExistingTaskItems_When_Search_Then_ReturnsFilteredPage()
    {
        using var client = CreateClient();

        // Seed
        await client.PostAsJsonAsync("/api/task-items",
            new TaskItemDto { Title = "SearchTarget Alpha", Priority = Priority.High });
        await client.PostAsJsonAsync("/api/task-items",
            new TaskItemDto { Title = "Other Beta", Priority = Priority.Low });

        var searchRequest = new SearchRequest<TaskItemSearchFilter>
        {
            PageIndex = 0,
            PageSize = 10,
            Filter = new TaskItemSearchFilter { SearchTerm = "SearchTarget" }
        };

        var response = await client.PostAsJsonAsync("/api/task-items/search", searchRequest);

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        var doc = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        var root = doc.RootElement;
        Assert.IsGreaterThanOrEqualTo(root.GetProperty("total").GetInt32(), 1);
        var data = root.GetProperty("data");
        Assert.IsTrue(data.EnumerateArray().Any(e => e.GetProperty("title").GetString()!.Contains("SearchTarget")));
    }

    [TestCategory("Endpoint")]
    [TestMethod]
    public async Task Given_EmptyDatabase_When_Search_Then_ReturnsEmptyPage()
    {
        using var client = CreateClient();
        var searchRequest = new SearchRequest<TaskItemSearchFilter>
        {
            PageIndex = 0,
            PageSize = 10,
            Filter = new TaskItemSearchFilter()
        };

        var response = await client.PostAsJsonAsync("/api/task-items/search", searchRequest);

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        var doc = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        var root = doc.RootElement;
        Assert.IsTrue(root.TryGetProperty("data", out _));
    }

    [TestCategory("Endpoint")]
    [TestMethod]
    public async Task Given_FullCrudCycle_When_AllOperationsExecuted_Then_AllSucceed()
    {
        using var client = CreateClient();

        // Create
        var dto = new TaskItemDto { Title = "CrudCycle", Priority = Priority.Critical };
        var createResponse = await client.PostAsJsonAsync("/api/task-items", dto);
        Assert.AreEqual(HttpStatusCode.Created, createResponse.StatusCode);
        var created = await createResponse.Content.ReadFromJsonAsync<TaskItemDto>();

        // Read
        var getResponse = await client.GetAsync($"/api/task-items/{created!.Id}");
        Assert.AreEqual(HttpStatusCode.OK, getResponse.StatusCode);

        // Update
        var updateDto = new TaskItemDto
        {
            Id = created.Id,
            Title = "CrudCycle Updated",
            Priority = Priority.Low,
            Status = created.Status
        };
        var updateResponse = await client.PutAsJsonAsync($"/api/task-items/{created.Id}", updateDto);
        Assert.AreEqual(HttpStatusCode.OK, updateResponse.StatusCode);

        // Delete
        var deleteResponse = await client.DeleteAsync($"/api/task-items/{created.Id}");
        Assert.AreEqual(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        // Verify deleted
        var verifyResponse = await client.GetAsync($"/api/task-items/{created.Id}");
        Assert.AreEqual(HttpStatusCode.NotFound, verifyResponse.StatusCode);
    }
}
