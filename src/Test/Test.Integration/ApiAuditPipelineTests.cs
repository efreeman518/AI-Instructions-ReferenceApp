using System.Net;
using System.Net.Http.Json;
using Aspire.Hosting.Testing;
using Azure;
using Azure.Data.Tables;
using EF.Common.Contracts;
using TaskFlow.Application.Models;
using TaskFlow.Infrastructure.Storage;

namespace Test.Integration;

/// <summary>
/// End-to-end audit pipeline test for the API: POST /api/categories → API request handling →
/// audit middleware → Azurite Table Storage row, with a polling read-back to confirm the persisted entity.
/// Aspire tier (Aspire.Hosting.Testing) — required because two Aspire resources participate
/// (<c>taskflowapi</c> for the request, <c>TableStorage1</c> for verification), and both must be Healthy
/// before the test can run. The polling helper tolerates eventual consistency between request completion
/// and table visibility.
/// </summary>
[TestClass]
[TestCategory("Integration")]
[DoNotParallelize]
public class ApiAuditPipelineTests
{
    private static readonly Guid ScaffoldTenantId = Guid.Parse("00000000-0000-0000-0000-000000000001");

    [TestMethod]
    [Timeout(300000)]
    public async Task Given_ApiCategoryCreate_When_RequestHandled_Then_AuditEntryPersistedToTableStorage()
    {
        var ct = CancellationToken.None;

        // Resources can be Running before they're actually serving requests — wait for the health check.
        await AspireTestHost.WaitForResourceHealthyAsync("taskflowapi", ct);
        await AspireTestHost.WaitForResourceHealthyAsync("TableStorage1", ct);

        using var client = AspireTestHost.AspireApp!.CreateHttpClient("taskflowapi", "http");
        client.Timeout = TimeSpan.FromMinutes(10);
        var auditWindowStartUtc = DateTimeOffset.UtcNow;
        var request = new DefaultRequest<CategoryDto>
        {
            Item = new CategoryDto
            {
                Name = $"Api Audit {Guid.NewGuid():N}",
                Description = "Integration-created category",
                SortOrder = 1,
                IsActive = true
            }
        };

        using var response = await PostCreateCategoryWithRetryAsync(client, request, ct);
        var responseBody = await response.Content.ReadFromJsonAsync<DefaultResponse<CategoryDto>>(cancellationToken: ct);

        Assert.AreEqual(HttpStatusCode.Created, response.StatusCode);
        Assert.IsNotNull(responseBody);
        Assert.IsNotNull(responseBody.Item);
        Assert.IsNotNull(responseBody.Item.Id);
        Assert.AreEqual(ScaffoldTenantId, responseBody.Item.TenantId);
        Assert.AreEqual(request.Item.Name, responseBody.Item.Name);

        var connectionString = await AspireTestHost.AspireApp!.GetConnectionStringAsync("TableStorage1", ct)
            .AsTask()
            .WaitAsync(AspireTestHost.DefaultTimeout, ct);
        var tableClient = new TableServiceClient(connectionString).GetTableClient("taskflowaudit");
        var auditEntity = await WaitForAuditEntityAsync(
            tableClient,
            ScaffoldTenantId.ToString(),
            auditWindowStartUtc,
            ct);

        Assert.IsNotNull(auditEntity);
        Assert.AreEqual(ScaffoldTenantId.ToString(), auditEntity.PartitionKey);
        Assert.AreEqual(ScaffoldTenantId.ToString(), auditEntity.TenantId);
        Assert.AreEqual("Category", auditEntity.EntityType);
        Assert.IsFalse(string.IsNullOrWhiteSpace(auditEntity.AuditId));
        Assert.AreEqual("Added", auditEntity.Action);
        Assert.AreEqual(AuditStatus.Success.ToString(), auditEntity.Status);
        Assert.IsTrue(auditEntity.RecordedUtc >= auditWindowStartUtc);
    }

    private static async Task<HttpResponseMessage> PostCreateCategoryWithRetryAsync(HttpClient client, object request, CancellationToken ct)
    {
        var deadline = DateTimeOffset.UtcNow.AddSeconds(45);
        HttpStatusCode? lastStatusCode = null;
        string? lastBody = null;
        Exception? lastException = null;

        while (DateTimeOffset.UtcNow < deadline)
        {
            try
            {
                var response = await client.PostAsJsonAsync("/api/categories", request, ct);
                if (response.StatusCode == HttpStatusCode.Created)
                    return response;

                lastStatusCode = response.StatusCode;
                lastBody = await response.Content.ReadAsStringAsync(ct);
                response.Dispose();
            }
            catch (HttpRequestException ex)
            {
                lastException = ex;
            }

            await Task.Delay(TimeSpan.FromSeconds(1), ct);
        }

        if (lastException != null)
            throw lastException;

        Assert.Fail($"Category create API did not return 201. Last status: {lastStatusCode}; body: {lastBody}");
        throw new InvalidOperationException("Unreachable");
    }

    private static async Task<AuditLogTableEntity> WaitForAuditEntityAsync(
        TableClient tableClient,
        string partitionKey,
        DateTimeOffset auditWindowStartUtc,
        CancellationToken ct)
    {
        var deadline = DateTimeOffset.UtcNow.AddSeconds(45);
        List<AuditLogTableEntity> recentEntities = [];

        while (DateTimeOffset.UtcNow < deadline)
        {
            try
            {
                recentEntities.Clear();

                await foreach (var entity in tableClient.QueryAsync<AuditLogTableEntity>(
                    entry => entry.PartitionKey == partitionKey,
                    cancellationToken: ct))
                {
                    if (entity.RecordedUtc < auditWindowStartUtc)
                        continue;

                    recentEntities.Add(entity);

                    if (entity.EntityType == "Category" &&
                        entity.Action == "Added" &&
                        entity.Status == AuditStatus.Success.ToString())
                    {
                        return entity;
                    }
                }
            }
            catch (RequestFailedException ex) when (ex.Status == 404)
            {
            }

            await Task.Delay(TimeSpan.FromSeconds(1), ct);
        }

        var recentEntitySummary = recentEntities.Count == 0
            ? "none"
            : string.Join(
                "; ",
                recentEntities
                    .OrderByDescending(entity => entity.RecordedUtc)
                    .Take(5)
                    .Select(entity => $"{entity.RecordedUtc:O}|{entity.EntityType}|{entity.Action}|{entity.Status}|{entity.AuditId}|{entity.EntityKey}"));

        Assert.Fail(
            $"Expected recent audit entity for partition '{partitionKey}' since '{auditWindowStartUtc:O}'. Recent entities: {recentEntitySummary}.");
        throw new InvalidOperationException("Unreachable");
    }
}