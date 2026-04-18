using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using EF.Common.Contracts;
using TaskFlow.Application.Models;

namespace Test.Endpoints;

[TestClass]
public class CategoryEndpointTests
{
    private static CustomApiFactory _factory = null!;

    [ClassInitialize]
    public static void ClassInit(TestContext _) => _factory = new CustomApiFactory();

    [ClassCleanup]
    public static void ClassCleanup() => _factory?.Dispose();

    private HttpClient CreateClient() => _factory.CreateClient();

    [TestCategory("Endpoint")]
    [TestMethod]
    public async Task Given_ValidPayload_When_PostCategory_Then_Returns201()
    {
        using var client = CreateClient();
        var dto = new CategoryDto { Name = "Test Category", SortOrder = 1, IsActive = true };

        var response = await client.PostAsJsonAsync("/api/categories", new DefaultRequest<CategoryDto> { Item = dto });

        Assert.AreEqual(HttpStatusCode.Created, response.StatusCode);
        var created = (await response.Content.ReadFromJsonAsync<DefaultResponse<CategoryDto>>())!.Item;
        Assert.IsNotNull(created);
        Assert.AreEqual("Test Category", created.Name);
        Assert.IsNotNull(created.Id);
    }

    [TestCategory("Endpoint")]
    [TestMethod]
    public async Task Given_ExistingCategory_When_GetById_Then_Returns200()
    {
        using var client = CreateClient();
        var dto = new CategoryDto { Name = "GetTest Category", SortOrder = 1, IsActive = true };
        var createResponse = await client.PostAsJsonAsync("/api/categories", new DefaultRequest<CategoryDto> { Item = dto });
        var created = (await createResponse.Content.ReadFromJsonAsync<DefaultResponse<CategoryDto>>())!.Item;

        var response = await client.GetAsync($"/api/categories/{created!.Id}");

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        var result = (await response.Content.ReadFromJsonAsync<DefaultResponse<CategoryDto>>())!.Item;
        Assert.IsNotNull(result);
        Assert.AreEqual("GetTest Category", result.Name);
    }

    [TestCategory("Endpoint")]
    [TestMethod]
    public async Task Given_NonExistentId_When_GetCategory_Then_Returns404()
    {
        using var client = CreateClient();

        var response = await client.GetAsync($"/api/categories/{Guid.NewGuid()}");

        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
    }

    [TestCategory("Endpoint")]
    [TestMethod]
    public async Task Given_ExistingCategory_When_PutUpdate_Then_Returns200()
    {
        using var client = CreateClient();
        var dto = new CategoryDto { Name = "Before", SortOrder = 1, IsActive = true };
        var createResponse = await client.PostAsJsonAsync("/api/categories", new DefaultRequest<CategoryDto> { Item = dto });
        var created = (await createResponse.Content.ReadFromJsonAsync<DefaultResponse<CategoryDto>>())!.Item;

        var updateDto = new CategoryDto
        {
            Id = created!.Id,
            Name = "After",
            SortOrder = 2,
            IsActive = true
        };
        var response = await client.PutAsJsonAsync($"/api/categories/{created.Id}", new DefaultRequest<CategoryDto> { Item = updateDto });

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        var updated = (await response.Content.ReadFromJsonAsync<DefaultResponse<CategoryDto>>())!.Item;
        Assert.AreEqual("After", updated!.Name);
    }

    [TestCategory("Endpoint")]
    [TestMethod]
    public async Task Given_ExistingCategory_When_Delete_Then_Returns204()
    {
        using var client = CreateClient();
        var dto = new CategoryDto { Name = "ToDelete Category", SortOrder = 1, IsActive = true };
        var createResponse = await client.PostAsJsonAsync("/api/categories", new DefaultRequest<CategoryDto> { Item = dto });
        var created = (await createResponse.Content.ReadFromJsonAsync<DefaultResponse<CategoryDto>>())!.Item;

        var response = await client.DeleteAsync($"/api/categories/{created!.Id}");

        Assert.AreEqual(HttpStatusCode.NoContent, response.StatusCode);

        var getResponse = await client.GetAsync($"/api/categories/{created.Id}");
        Assert.AreEqual(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    [TestCategory("Endpoint")]
    [TestMethod]
    public async Task Given_ExistingCategories_When_Search_Then_ReturnsFilteredPage()
    {
        using var client = CreateClient();

        await client.PostAsJsonAsync("/api/categories",
            new DefaultRequest<CategoryDto> { Item = new CategoryDto { Name = "SearchMe Cat", SortOrder = 1, IsActive = true } });
        await client.PostAsJsonAsync("/api/categories",
            new DefaultRequest<CategoryDto> { Item = new CategoryDto { Name = "Other Cat", SortOrder = 2, IsActive = true } });

        var searchRequest = new SearchRequest<CategorySearchFilter>
        {
            PageIndex = 0,
            PageSize = 10,
            Filter = new CategorySearchFilter { SearchTerm = "SearchMe" }
        };

        var response = await client.PostAsJsonAsync("/api/categories/search", searchRequest);

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        var doc = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        var root = doc.RootElement;
        Assert.IsGreaterThanOrEqualTo(root.GetProperty("total").GetInt32(), 1);
        var data = root.GetProperty("data");
        Assert.IsTrue(data.EnumerateArray().Any(e => e.GetProperty("name").GetString()!.Contains("SearchMe")));
    }

    [TestCategory("Endpoint")]
    [TestMethod]
    public async Task Given_FullCrudCycle_When_AllOperationsExecuted_Then_AllSucceed()
    {
        using var client = CreateClient();

        // Create
        var dto = new CategoryDto { Name = "CrudCycle Cat", SortOrder = 1, IsActive = true };
        var createResponse = await client.PostAsJsonAsync("/api/categories", new DefaultRequest<CategoryDto> { Item = dto });
        Assert.AreEqual(HttpStatusCode.Created, createResponse.StatusCode);
        var created = (await createResponse.Content.ReadFromJsonAsync<DefaultResponse<CategoryDto>>())!.Item;

        // Read
        var getResponse = await client.GetAsync($"/api/categories/{created!.Id}");
        Assert.AreEqual(HttpStatusCode.OK, getResponse.StatusCode);

        // Update
        var updateDto = new CategoryDto
        {
            Id = created.Id,
            Name = "CrudCycle Cat Updated",
            SortOrder = 5,
            IsActive = false
        };
        var updateResponse = await client.PutAsJsonAsync($"/api/categories/{created.Id}", new DefaultRequest<CategoryDto> { Item = updateDto });
        Assert.AreEqual(HttpStatusCode.OK, updateResponse.StatusCode);

        // Delete
        var deleteResponse = await client.DeleteAsync($"/api/categories/{created.Id}");
        Assert.AreEqual(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        // Verify deleted
        var verifyResponse = await client.GetAsync($"/api/categories/{created.Id}");
        Assert.AreEqual(HttpStatusCode.NotFound, verifyResponse.StatusCode);
    }
}
