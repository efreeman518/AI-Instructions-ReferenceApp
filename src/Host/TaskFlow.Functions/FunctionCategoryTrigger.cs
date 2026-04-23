using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using TaskFlow.Application.Contracts.Services;
using TaskFlow.Application.Models;

namespace TaskFlow.Functions;

public class FunctionCategoryTrigger(
    ILogger<FunctionCategoryTrigger> logger,
    ICategoryService categoryService)
{
    [Function(nameof(CreateCategory))]
    public async Task<HttpResponseData> CreateCategory(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "categories")] HttpRequestData req,
        CancellationToken ct)
    {
        var request = await req.ReadFromJsonAsync<CreateCategoryRequest>(cancellationToken: ct);
        if (request == null || string.IsNullOrWhiteSpace(request.Name))
        {
            var badRequest = req.CreateResponse(HttpStatusCode.BadRequest);
            await badRequest.WriteAsJsonAsync(new { message = "Name is required." }, ct);
            return badRequest;
        }

        var result = await categoryService.CreateAsync(new DefaultRequest<CategoryDto>
        {
            Item = new CategoryDto
            {
                Name = request.Name.Trim(),
                Description = request.Description,
                SortOrder = request.SortOrder,
                IsActive = request.IsActive
            }
        }, ct);

        if (result.IsFailure || result.Value?.Item == null)
        {
            logger.LogWarning("CreateCategory failed for request {Name}", request.Name);
            var failed = req.CreateResponse(HttpStatusCode.BadRequest);
            await failed.WriteAsJsonAsync(new { message = "Unable to create category." }, ct);
            return failed;
        }

        logger.LogInformation("CreateCategory created {CategoryId}", result.Value.Item.Id);

        var response = req.CreateResponse(HttpStatusCode.Created);
        await response.WriteAsJsonAsync(result.Value.Item, ct);
        return response;
    }

    public sealed record CreateCategoryRequest
    {
        public string Name { get; init; } = null!;
        public string? Description { get; init; }
        public int SortOrder { get; init; }
        public bool IsActive { get; init; } = true;
    }
}