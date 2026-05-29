using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using TaskFlow.Application.Models;
using TaskFlow.Domain.Shared.Enums;

namespace Test.Endpoints;

/// <summary>
/// HTTP contract tests for <c>/api/v1/comments</c> CRUD; each test seeds a parent TaskItem first.
/// Endpoint tier (WebApplicationFactory + EF InMemory via <c>CustomApiFactory</c>): contract-level
/// coverage - status codes, envelope shape, and 404 paths.
/// </summary>
[TestClass]
public class CommentEndpointTests
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

    /// <summary>Creates parent task item used by the surrounding test cases.</summary>
    private async Task<Guid> CreateParentTaskItem(HttpClient client)
    {
        var dto = new TaskItemDto { Title = "ParentForComment", Priority = Priority.Medium };
        var response = await client.PostAsJsonAsync("/api/v1/task-items", new DefaultRequest<TaskItemDto> { Item = dto });
        var created = (await response.Content.ReadFromJsonAsync<DefaultResponse<TaskItemDto>>(_jsonOptions))!.Item;
        return created!.Id!.Value;
    }

    /// <summary>Verifies that given valid payload, when post comment, then returns 201.</summary>
    [TestCategory("Endpoint")]
    [TestMethod]
    public async Task Given_ValidPayload_When_PostComment_Then_Returns201()
    {
        using var client = CreateClient();
        var taskId = await CreateParentTaskItem(client);
        var dto = new CommentDto { Body = "Test comment", TaskItemId = taskId };

        var response = await client.PostAsJsonAsync("/api/v1/comments", new DefaultRequest<CommentDto> { Item = dto });

        Assert.AreEqual(HttpStatusCode.Created, response.StatusCode);
        var created = (await response.Content.ReadFromJsonAsync<DefaultResponse<CommentDto>>())!.Item;
        Assert.IsNotNull(created);
        Assert.AreEqual("Test comment", created.Body);
    }

    /// <summary>Verifies that given existing comment, when get by ID, then returns 200.</summary>
    [TestCategory("Endpoint")]
    [TestMethod]
    public async Task Given_ExistingComment_When_GetById_Then_Returns200()
    {
        using var client = CreateClient();
        var taskId = await CreateParentTaskItem(client);
        var dto = new CommentDto { Body = "GetComment body", TaskItemId = taskId };
        var createResponse = await client.PostAsJsonAsync("/api/v1/comments", new DefaultRequest<CommentDto> { Item = dto });
        var created = (await createResponse.Content.ReadFromJsonAsync<DefaultResponse<CommentDto>>())!.Item;

        var response = await client.GetAsync($"/api/v1/comments/{created!.Id}");

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        var result = (await response.Content.ReadFromJsonAsync<DefaultResponse<CommentDto>>())!.Item;
        Assert.IsNotNull(result);
        Assert.AreEqual("GetComment body", result.Body);
    }

    /// <summary>Verifies that given non existent ID, when get comment, then returns 404.</summary>
    [TestCategory("Endpoint")]
    [TestMethod]
    public async Task Given_NonExistentId_When_GetComment_Then_Returns404()
    {
        using var client = CreateClient();

        var response = await client.GetAsync($"/api/v1/comments/{Guid.NewGuid()}");

        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
    }

    /// <summary>Verifies that given existing comment, when put update, then returns 200.</summary>
    [TestCategory("Endpoint")]
    [TestMethod]
    public async Task Given_ExistingComment_When_PutUpdate_Then_Returns200()
    {
        using var client = CreateClient();
        var taskId = await CreateParentTaskItem(client);
        var dto = new CommentDto { Body = "Before update", TaskItemId = taskId };
        var createResponse = await client.PostAsJsonAsync("/api/v1/comments", new DefaultRequest<CommentDto> { Item = dto });
        var created = (await createResponse.Content.ReadFromJsonAsync<DefaultResponse<CommentDto>>())!.Item;

        var updateDto = new CommentDto { Id = created!.Id, Body = "After update", TaskItemId = taskId };
        var response = await client.PutAsJsonAsync($"/api/v1/comments/{created.Id}", new DefaultRequest<CommentDto> { Item = updateDto });

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        var updated = (await response.Content.ReadFromJsonAsync<DefaultResponse<CommentDto>>())!.Item;
        Assert.AreEqual("After update", updated!.Body);
    }

    /// <summary>Verifies that given existing comment, when delete, then returns 204.</summary>
    [TestCategory("Endpoint")]
    [TestMethod]
    public async Task Given_ExistingComment_When_Delete_Then_Returns204()
    {
        using var client = CreateClient();
        var taskId = await CreateParentTaskItem(client);
        var dto = new CommentDto { Body = "ToDelete comment", TaskItemId = taskId };
        var createResponse = await client.PostAsJsonAsync("/api/v1/comments", new DefaultRequest<CommentDto> { Item = dto });
        var created = (await createResponse.Content.ReadFromJsonAsync<DefaultResponse<CommentDto>>())!.Item;

        var response = await client.DeleteAsync($"/api/v1/comments/{created!.Id}");

        Assert.AreEqual(HttpStatusCode.NoContent, response.StatusCode);

        var getResponse = await client.GetAsync($"/api/v1/comments/{created.Id}");
        Assert.AreEqual(HttpStatusCode.NotFound, getResponse.StatusCode);
    }
}
