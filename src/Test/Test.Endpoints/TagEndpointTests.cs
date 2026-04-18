using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using EF.Common.Contracts;
using TaskFlow.Application.Models;

namespace Test.Endpoints;

[TestClass]
public class TagEndpointTests
{
    private static CustomApiFactory _factory = null!;

    [ClassInitialize]
    public static void ClassInit(TestContext _) => _factory = new CustomApiFactory();

    [ClassCleanup]
    public static void ClassCleanup() => _factory?.Dispose();

    private HttpClient CreateClient() => _factory.CreateClient();

    [TestCategory("Endpoint")]
    [TestMethod]
    public async Task Given_ValidPayload_When_PostTag_Then_Returns201()
    {
        using var client = CreateClient();
        var dto = new TagDto { Name = "Urgent", Color = "#FF0000" };

        var response = await client.PostAsJsonAsync("/api/tags", new DefaultRequest<TagDto> { Item = dto });

        Assert.AreEqual(HttpStatusCode.Created, response.StatusCode);
        var created = (await response.Content.ReadFromJsonAsync<DefaultResponse<TagDto>>())!.Item;
        Assert.IsNotNull(created);
        Assert.AreEqual("Urgent", created.Name);
        Assert.IsNotNull(created.Id);
    }

    [TestCategory("Endpoint")]
    [TestMethod]
    public async Task Given_ExistingTag_When_GetById_Then_Returns200()
    {
        using var client = CreateClient();
        var dto = new TagDto { Name = "GetTag", Color = "#00FF00" };
        var createResponse = await client.PostAsJsonAsync("/api/tags", new DefaultRequest<TagDto> { Item = dto });
        var created = (await createResponse.Content.ReadFromJsonAsync<DefaultResponse<TagDto>>())!.Item;

        var response = await client.GetAsync($"/api/tags/{created!.Id}");

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        var result = (await response.Content.ReadFromJsonAsync<DefaultResponse<TagDto>>())!.Item;
        Assert.IsNotNull(result);
        Assert.AreEqual("GetTag", result.Name);
    }

    [TestCategory("Endpoint")]
    [TestMethod]
    public async Task Given_NonExistentId_When_GetTag_Then_Returns404()
    {
        using var client = CreateClient();

        var response = await client.GetAsync($"/api/tags/{Guid.NewGuid()}");

        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
    }

    [TestCategory("Endpoint")]
    [TestMethod]
    public async Task Given_ExistingTag_When_PutUpdate_Then_Returns200()
    {
        using var client = CreateClient();
        var dto = new TagDto { Name = "BeforeTag", Color = "#111111" };
        var createResponse = await client.PostAsJsonAsync("/api/tags", new DefaultRequest<TagDto> { Item = dto });
        var created = (await createResponse.Content.ReadFromJsonAsync<DefaultResponse<TagDto>>())!.Item;

        var updateDto = new TagDto { Id = created!.Id, Name = "AfterTag", Color = "#222222" };
        var response = await client.PutAsJsonAsync($"/api/tags/{created.Id}", new DefaultRequest<TagDto> { Item = updateDto });

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        var updated = (await response.Content.ReadFromJsonAsync<DefaultResponse<TagDto>>())!.Item;
        Assert.AreEqual("AfterTag", updated!.Name);
    }

    [TestCategory("Endpoint")]
    [TestMethod]
    public async Task Given_ExistingTag_When_Delete_Then_Returns204()
    {
        using var client = CreateClient();
        var dto = new TagDto { Name = "ToDeleteTag", Color = "#333333" };
        var createResponse = await client.PostAsJsonAsync("/api/tags", new DefaultRequest<TagDto> { Item = dto });
        var created = (await createResponse.Content.ReadFromJsonAsync<DefaultResponse<TagDto>>())!.Item;

        var response = await client.DeleteAsync($"/api/tags/{created!.Id}");

        Assert.AreEqual(HttpStatusCode.NoContent, response.StatusCode);

        var getResponse = await client.GetAsync($"/api/tags/{created.Id}");
        Assert.AreEqual(HttpStatusCode.NotFound, getResponse.StatusCode);
    }
}
