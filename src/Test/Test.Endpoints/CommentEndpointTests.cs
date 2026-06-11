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

    // Comments are internal to the TaskItem aggregate (GR-15): they are created, updated, and removed
    // only through the nested /task-items/{id}/comments routes on the root, never a standalone
    // /comments write route. Reads still live on /comments.

    /// <summary>Verifies that given non existent ID, when get comment, then returns 404.</summary>
    [TestCategory("Endpoint")]
    [TestMethod]
    public async Task Given_NonExistentId_When_GetComment_Then_Returns404()
    {
        using var client = CreateClient();

        var response = await client.GetAsync($"/api/v1/comments/{Guid.NewGuid()}");

        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
    }

    /// <summary>Verifies that adding a comment through the TaskItem root returns 201 and is readable.</summary>
    [TestCategory("Endpoint")]
    [TestMethod]
    public async Task Given_ValidPayload_When_AddCommentToTaskItem_Then_Returns201AndReadable()
    {
        using var client = CreateClient();
        var taskId = await CreateParentTaskItem(client);

        var dto = new CommentDto { Body = "Nested add", TaskItemId = taskId };
        var addResp = await client.PostAsJsonAsync($"/api/v1/task-items/{taskId}/comments",
            new DefaultRequest<CommentDto> { Item = dto });
        Assert.AreEqual(HttpStatusCode.Created, addResp.StatusCode,
            $"Add failed: {await addResp.Content.ReadAsStringAsync()}");

        var created = (await addResp.Content.ReadFromJsonAsync<DefaultResponse<CommentDto>>(_jsonOptions))!.Item;
        Assert.IsNotNull(created);
        Assert.AreEqual("Nested add", created.Body);

        var getResp = await client.GetAsync($"/api/v1/comments/{created.Id}");
        Assert.AreEqual(HttpStatusCode.OK, getResp.StatusCode);
    }

    /// <summary>Verifies the add/update/remove comment lifecycle through the TaskItem root.</summary>
    [TestCategory("Endpoint")]
    [TestMethod]
    public async Task Given_Comment_When_UpdatedAndRemovedThroughRoot_Then_ReflectsState()
    {
        using var client = CreateClient();
        var taskId = await CreateParentTaskItem(client);

        var addResp = await client.PostAsJsonAsync($"/api/v1/task-items/{taskId}/comments",
            new DefaultRequest<CommentDto> { Item = new CommentDto { Body = "Original", TaskItemId = taskId } });
        var commentId = (await addResp.Content.ReadFromJsonAsync<DefaultResponse<CommentDto>>(_jsonOptions))!.Item!.Id!.Value;

        var updResp = await client.PutAsJsonAsync($"/api/v1/task-items/{taskId}/comments/{commentId}",
            new DefaultRequest<CommentDto> { Item = new CommentDto { Body = "Edited", TaskItemId = taskId } });
        Assert.AreEqual(HttpStatusCode.OK, updResp.StatusCode);
        var updated = (await updResp.Content.ReadFromJsonAsync<DefaultResponse<CommentDto>>(_jsonOptions))!.Item;
        Assert.AreEqual("Edited", updated!.Body);

        var delResp = await client.DeleteAsync($"/api/v1/task-items/{taskId}/comments/{commentId}");
        Assert.AreEqual(HttpStatusCode.NoContent, delResp.StatusCode);

        var getResp = await client.GetAsync($"/api/v1/comments/{commentId}");
        Assert.AreEqual(HttpStatusCode.NotFound, getResp.StatusCode);
    }

    /// <summary>Verifies that adding a comment to a missing TaskItem returns 404.</summary>
    [TestCategory("Endpoint")]
    [TestMethod]
    public async Task Given_MissingTaskItem_When_AddComment_Then_Returns404()
    {
        using var client = CreateClient();

        var response = await client.PostAsJsonAsync($"/api/v1/task-items/{Guid.NewGuid()}/comments",
            new DefaultRequest<CommentDto> { Item = new CommentDto { Body = "Orphan" } });

        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
    }
}
