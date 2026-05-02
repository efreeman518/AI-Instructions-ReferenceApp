using System.Net;
using System.Net.Http.Json;
using Aspire.Hosting.Testing;
using Azure;
using Azure.Data.Tables;
using EF.Common.Contracts;
using TaskFlow.Application.Models;
using TaskFlow.Infrastructure.Storage;

namespace Test.Integration;

[TestClass]
[TestCategory("Integration")]
[DoNotParallelize]
public class FunctionAuditPipelineTests
{
    private static readonly Guid FunctionFallbackTenantId = Guid.Parse("00000000-0000-0000-0000-000000000001");

    [TestMethod]
    [Timeout(300000)]
    public async Task Given_FunctionCategoryCreate_When_RequestHandled_Then_AuditEntryPersistedToTableStorage()
    {
        if (!DatabaseFixture.EnsureFuncToolAvailable())
        {
            Assert.Inconclusive("Azure Functions Core Tools ('func') is required to run the end-to-end Functions audit pipeline test.");
        }

        using var client = DatabaseFixture.AspireApp!.CreateHttpClient("taskflowfunctions", "http");
        client.Timeout = TimeSpan.FromMinutes(10);
        var auditWindowStartUtc = DateTimeOffset.UtcNow;
        var request = new
        {
            Name = $"Function Audit {Guid.NewGuid():N}",
            Description = "Integration-created category"
        };

        using var response = await PostCreateCategoryWithRetryAsync(client, request, CancellationToken.None);
        var responseBody = await response.Content.ReadFromJsonAsync<CategoryDto>(cancellationToken: CancellationToken.None);

        Assert.AreEqual(HttpStatusCode.Created, response.StatusCode);
        Assert.IsNotNull(responseBody);
        Assert.IsNotNull(responseBody.Id);
        Assert.AreEqual(FunctionFallbackTenantId, responseBody.TenantId);
        Assert.AreEqual(request.Name, responseBody.Name);

        var connectionString = await DatabaseFixture.AspireApp!.GetConnectionStringAsync("TableStorage1");
        var tableClient = new TableServiceClient(connectionString).GetTableClient("taskflowaudit");
        var auditEntity = await WaitForAuditEntityAsync(
            tableClient,
            FunctionFallbackTenantId.ToString(),
            auditWindowStartUtc,
            CancellationToken.None);

        Assert.IsNotNull(auditEntity);
        Assert.AreEqual(FunctionFallbackTenantId.ToString(), auditEntity.PartitionKey);
        Assert.AreEqual(FunctionFallbackTenantId.ToString(), auditEntity.TenantId);
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

        Assert.Fail($"Category create function did not return 201. Last status: {lastStatusCode}; body: {lastBody}");
        throw new InvalidOperationException("Unreachable");
    }

    private static async Task<AuditLogTableEntity> WaitForAuditEntityAsync(
        TableClient tableClient,
        string partitionKey,
        DateTimeOffset auditWindowStartUtc,
        CancellationToken ct)
    {
        var deadline = DateTimeOffset.UtcNow.AddSeconds(45);
        List<AuditLogTableEntity> recentPartitionEntities = [];
        List<AuditLogTableEntity> recentEntitiesAcrossPartitions = [];

        while (DateTimeOffset.UtcNow < deadline)
        {
            try
            {
                recentPartitionEntities.Clear();
                recentEntitiesAcrossPartitions.Clear();

                await foreach (var entity in tableClient.QueryAsync<AuditLogTableEntity>(
                    cancellationToken: ct))
                {
                    if (entity.RecordedUtc < auditWindowStartUtc)
                        continue;

                    recentEntitiesAcrossPartitions.Add(entity);

                    if (entity.PartitionKey == partitionKey)
                        recentPartitionEntities.Add(entity);

                    if (entity.PartitionKey == partitionKey &&
                        entity.EntityType == "Category" &&
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

        var recentPartitionSummary = recentPartitionEntities.Count == 0
            ? "none"
            : string.Join(
                "; ",
                recentPartitionEntities
                    .OrderByDescending(entity => entity.RecordedUtc)
                    .Take(5)
                    .Select(entity => $"{entity.PartitionKey}|{entity.RecordedUtc:O}|{entity.EntityType}|{entity.Action}|{entity.Status}|{entity.AuditId}|{entity.EntityKey}"));

        var recentGlobalSummary = recentEntitiesAcrossPartitions.Count == 0
            ? "none"
            : string.Join(
                "; ",
                recentEntitiesAcrossPartitions
                    .OrderByDescending(entity => entity.RecordedUtc)
                    .Take(10)
                    .Select(entity => $"{entity.PartitionKey}|{entity.RecordedUtc:O}|{entity.EntityType}|{entity.Action}|{entity.Status}|{entity.AuditId}|{entity.EntityKey}"));

        Assert.Fail(
            $"Expected recent audit entity for partition '{partitionKey}' since '{auditWindowStartUtc:O}'. Recent partition entities: {recentPartitionSummary}. Recent entities across all partitions: {recentGlobalSummary}.");
        throw new InvalidOperationException("Unreachable");
    }
}