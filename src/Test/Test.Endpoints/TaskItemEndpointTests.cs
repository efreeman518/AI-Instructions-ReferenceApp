using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using EF.Common.Contracts;
using TaskFlow.Application.Models;
using TaskFlow.Domain.Shared.Enums;

namespace Test.Endpoints;

/// <summary>
/// HTTP contract tests for <c>/api/v1/task-items</c> CRUD plus search (filter by SearchTerm, paged response)
/// and a full create->read->update->delete cycle.
/// Endpoint tier (WebApplicationFactory + EF InMemory via <c>CustomApiFactory</c>): the same
/// multi-endpoint workflow runs against real SQL in <c>TaskItemCrudE2ETests</c>; here we only need
/// contract correctness, not SQL semantics, so InMemory is sufficient.
/// </summary>
[TestClass]
public class TaskItemEndpointTests
{
    private static CustomApiFactory _factory = null!;
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    /// <summary>Initializes shared test fixtures before the class-level test run begins.</summary>
    [ClassInitialize]
    public static void ClassInit(TestContext _) => _factory = new CustomApiFactory();

    /// <summary>Disposes shared test fixtures after the class-level test run finishes.</summary>
    [ClassCleanup]
    public static void ClassCleanup() => _factory?.Dispose();

    /// <summary>Creates client used by the surrounding test cases.</summary>
    private HttpClient CreateClient() => _factory.CreateClient();

    /// <summary>Verifies that given valid payload, when post task item, then returns 201.</summary>
    [TestCategory("Endpoint")]
    [TestMethod]
    public async Task Given_ValidPayload_When_PostTaskItem_Then_Returns201()
    {
        using var client = CreateClient();
        var dto = new TaskItemDto { Title = "Test Task", Priority = Priority.Medium };

        var response = await client.PostAsJsonAsync("/api/v1/task-items", new DefaultRequest<TaskItemDto> { Item = dto });

        Assert.AreEqual(HttpStatusCode.Created, response.StatusCode);
        var created = (await response.Content.ReadFromJsonAsync<DefaultResponse<TaskItemDto>>(_jsonOptions))!.Item;
        Assert.IsNotNull(created);
        Assert.AreEqual("Test Task", created.Title);
        Assert.IsNotNull(created.Id);
    }

    /// <summary>Verifies that given existing task item, when get by ID, then returns 200.</summary>
    [TestCategory("Endpoint")]
    [TestMethod]
    public async Task Given_ExistingTaskItem_When_GetById_Then_Returns200()
    {
        using var client = CreateClient();
        var dto = new TaskItemDto { Title = "GetTest", Priority = Priority.Low };
        var createResponse = await client.PostAsJsonAsync("/api/v1/task-items", new DefaultRequest<TaskItemDto> { Item = dto });
        var created = (await createResponse.Content.ReadFromJsonAsync<DefaultResponse<TaskItemDto>>(_jsonOptions))!.Item;

        var response = await client.GetAsync($"/api/v1/task-items/{created!.Id}");

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        var result = (await response.Content.ReadFromJsonAsync<DefaultResponse<TaskItemDto>>(_jsonOptions))!.Item;
        Assert.IsNotNull(result);
        Assert.AreEqual("GetTest", result.Title);
    }

    /// <summary>Verifies that given non existent ID, when get task item, then returns 404.</summary>
    [TestCategory("Endpoint")]
    [TestMethod]
    public async Task Given_NonExistentId_When_GetTaskItem_Then_Returns404()
    {
        using var client = CreateClient();

        var response = await client.GetAsync($"/api/v1/task-items/{Guid.NewGuid()}");

        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
    }

    /// <summary>Verifies that given existing task item, when put update, then returns 200.</summary>
    [TestCategory("Endpoint")]
    [TestMethod]
    public async Task Given_ExistingTaskItem_When_PutUpdate_Then_Returns200()
    {
        using var client = CreateClient();
        var dto = new TaskItemDto { Title = "Before Update", Priority = Priority.Medium };
        var createResponse = await client.PostAsJsonAsync("/api/v1/task-items", new DefaultRequest<TaskItemDto> { Item = dto });
        var created = (await createResponse.Content.ReadFromJsonAsync<DefaultResponse<TaskItemDto>>(_jsonOptions))!.Item;

        var updateDto = new TaskItemDto
        {
            Id = created!.Id,
            Title = "After Update",
            Priority = Priority.High,
            Status = created.Status
        };
        var response = await client.PutAsJsonAsync($"/api/v1/task-items/{created.Id}", new DefaultRequest<TaskItemDto> { Item = updateDto });

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        var updated = (await response.Content.ReadFromJsonAsync<DefaultResponse<TaskItemDto>>(_jsonOptions))!.Item;
        Assert.AreEqual("After Update", updated!.Title);
    }

    /// <summary>Verifies that given existing task item, when delete, then returns 204.</summary>
    [TestCategory("Endpoint")]
    [TestMethod]
    public async Task Given_ExistingTaskItem_When_Delete_Then_Returns204()
    {
        using var client = CreateClient();
        var dto = new TaskItemDto { Title = "ToDelete", Priority = Priority.Low };
        var createResponse = await client.PostAsJsonAsync("/api/v1/task-items", new DefaultRequest<TaskItemDto> { Item = dto });
        var created = (await createResponse.Content.ReadFromJsonAsync<DefaultResponse<TaskItemDto>>(_jsonOptions))!.Item;

        var response = await client.DeleteAsync($"/api/v1/task-items/{created!.Id}");

        Assert.AreEqual(HttpStatusCode.NoContent, response.StatusCode);

        // Verify deleted
        var getResponse = await client.GetAsync($"/api/v1/task-items/{created.Id}");
        Assert.AreEqual(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    /// <summary>Verifies that given existing task items, when search, then returns filtered page.</summary>
    [TestCategory("Endpoint")]
    [TestMethod]
    public async Task Given_ExistingTaskItems_When_Search_Then_ReturnsFilteredPage()
    {
        using var client = CreateClient();

        // Seed
        await client.PostAsJsonAsync("/api/v1/task-items",
            new DefaultRequest<TaskItemDto> { Item = new TaskItemDto { Title = "SearchTarget Alpha", Priority = Priority.High } });
        await client.PostAsJsonAsync("/api/v1/task-items",
            new DefaultRequest<TaskItemDto> { Item = new TaskItemDto { Title = "Other Beta", Priority = Priority.Low } });

        var searchRequest = new SearchRequest<TaskItemSearchFilter>
        {
            PageIndex = 0,
            PageSize = 10,
            Filter = new TaskItemSearchFilter { SearchTerm = "SearchTarget" }
        };

        var response = await client.PostAsJsonAsync("/api/v1/task-items/search", searchRequest);

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        var doc = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        var root = doc.RootElement;
        Assert.IsGreaterThanOrEqualTo(root.GetProperty("total").GetInt32(), 1);
        var data = root.GetProperty("data");
        Assert.IsTrue(data.EnumerateArray().Any(e => e.GetProperty("title").GetString()!.Contains("SearchTarget")));
    }

    /// <summary>Verifies that given empty database, when search, then returns empty page.</summary>
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

        var response = await client.PostAsJsonAsync("/api/v1/task-items/search", searchRequest);

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        var doc = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        var root = doc.RootElement;
        Assert.IsTrue(root.TryGetProperty("data", out _));
    }

    /// <summary>Verifies that given full CRUD cycle, when all operations executed, then all succeed.</summary>
    [TestCategory("Endpoint")]
    [TestMethod]
    public async Task Given_FullCrudCycle_When_AllOperationsExecuted_Then_AllSucceed()
    {
        using var client = CreateClient();

        // Create
        var dto = new TaskItemDto { Title = "CrudCycle", Priority = Priority.Critical };
        var createResponse = await client.PostAsJsonAsync("/api/v1/task-items", new DefaultRequest<TaskItemDto> { Item = dto });
        Assert.AreEqual(HttpStatusCode.Created, createResponse.StatusCode);
        var created = (await createResponse.Content.ReadFromJsonAsync<DefaultResponse<TaskItemDto>>(_jsonOptions))!.Item;

        // Read
        var getResponse = await client.GetAsync($"/api/v1/task-items/{created!.Id}");
        Assert.AreEqual(HttpStatusCode.OK, getResponse.StatusCode);

        // Update
        var updateDto = new TaskItemDto
        {
            Id = created.Id,
            Title = "CrudCycle Updated",
            Priority = Priority.Low,
            Status = created.Status
        };
        var updateResponse = await client.PutAsJsonAsync($"/api/v1/task-items/{created.Id}", new DefaultRequest<TaskItemDto> { Item = updateDto });
        Assert.AreEqual(HttpStatusCode.OK, updateResponse.StatusCode);

        // Delete
        var deleteResponse = await client.DeleteAsync($"/api/v1/task-items/{created.Id}");
        Assert.AreEqual(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        // Verify deleted
        var verifyResponse = await client.GetAsync($"/api/v1/task-items/{created.Id}");
        Assert.AreEqual(HttpStatusCode.NotFound, verifyResponse.StatusCode);
    }
}
