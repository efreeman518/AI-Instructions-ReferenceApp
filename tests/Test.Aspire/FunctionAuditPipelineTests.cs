using Aspire.Hosting.Testing;
using Azure;
using Azure.Data.Tables;
using EF.Common.Contracts;
using EF.IntegrationTesting.Aspire;
using System.Net;
using System.Net.Http.Json;
using TaskFlow.Application.Models;
using TaskFlow.Infrastructure.Storage;

namespace Test.Aspire;

/// <summary>
/// End-to-end audit pipeline test for the Functions host: POST /api/v1/categories on the
/// <c>taskflowfunctions</c> resource -> Function request handling -> audit middleware -> Azurite Table
/// Storage row, with a polling read-back.
/// Aspire tier (Aspire.Hosting.Testing) - required because the Functions host has the longest cold-start
/// of any resource and the test depends on both <c>taskflowfunctions</c> and <c>TableStorage1</c>. Missing
/// Core Tools fails unless <c>TASKFLOW_RUN_FUNCTIONS_TESTS=false</c> explicitly opts out.
/// </summary>
[TestClass]
[TestCategory("Aspire")]
[DoNotParallelize]
public class FunctionAuditPipelineTests
{
    private static readonly Guid FunctionFallbackTenantId = Guid.Parse("00000000-0000-0000-0000-000000000001");

    /// <summary>Boots the Aspire graph lazily on first mesh-test class to run; teardown is owned by <c>AspireMeshLifecycle</c>.</summary>
    [ClassInitialize]
    public static Task ClassInit(TestContext context) => AspireTestHost.EnsureStartedAsync(context);

    /// <summary>Verifies that given function category create, when request handled, then audit entry persisted to table storage.</summary>
    [TestMethod]
    [Timeout(1_200_000, CooperativeCancellation = true)]
    public async Task Given_FunctionCategoryCreate_When_RequestHandled_Then_AuditEntryPersistedToTableStorage()
    {
        if (string.Equals(
                Environment.GetEnvironmentVariable(AspireTestHost.RunFunctionsTestsEnvironmentVariable),
                "false",
                StringComparison.OrdinalIgnoreCase))
        {
            Assert.Inconclusive($"{AspireTestHost.RunFunctionsTestsEnvironmentVariable}=false - Functions full-stack test opted out.");
            return;
        }

        if (!AspireTestHost.EnsureFuncToolAvailable())
        {
            Assert.Fail("Azure Functions Core Tools ('func') is required. Install it or set TASKFLOW_RUN_FUNCTIONS_TESTS=false to opt out explicitly.");
            return;
        }

        var ct = CancellationToken.None;

        // Functions host has the longest cold-start of any resource - wait for health before issuing requests.
        await AspireTestHost.WaitForResourceHealthyAsync("taskflowfunctions", ct);
        await AspireTestHost.WaitForResourceHealthyAsync("TableStorage1", ct);

        try
        {
            using var client = AspireTestHost.AspireApp!.CreateHttpClient("taskflowfunctions", "http");
            client.Timeout = TimeSpan.FromMinutes(10);
            await AspireTestHost.RunStartupStepAsync(
                "Functions HTTP readiness",
                token => WaitForFunctionReadyAsync(client, token),
                ct);

            var auditWindowStartUtc = DateTimeOffset.UtcNow;
            var request = new
            {
                Name = $"Function Audit {Guid.NewGuid():N}",
                Description = "Integration-created category"
            };

            using var response = await PostCreateCategoryWithRetryAsync(client, request, ct);
            var responseBody = await response.Content.ReadFromJsonAsync<CategoryDto>(cancellationToken: ct);

            Assert.AreEqual(HttpStatusCode.Created, response.StatusCode);
            Assert.IsNotNull(responseBody);
            Assert.IsNotNull(responseBody.Id);
            Assert.AreEqual(FunctionFallbackTenantId, responseBody.TenantId);
            Assert.AreEqual(request.Name, responseBody.Name);

            var connectionString = await AspireTestHost.AspireApp!.GetRequiredConnectionStringAsync(
                "TableStorage1",
                AspireTestHost.DefaultTimeout,
                ct);
            var tableClient = new TableServiceClient(connectionString).GetTableClient("taskflowaudit");
            var auditEntity = await WaitForAuditEntityAsync(
                tableClient,
                FunctionFallbackTenantId.ToString(),
                auditWindowStartUtc,
                ct);

            Assert.IsNotNull(auditEntity);
            Assert.AreEqual(FunctionFallbackTenantId.ToString(), auditEntity.PartitionKey);
            Assert.AreEqual(FunctionFallbackTenantId.ToString(), auditEntity.TenantId);
            Assert.AreEqual("Category", auditEntity.EntityType);
            Assert.IsFalse(string.IsNullOrWhiteSpace(auditEntity.AuditId));
            Assert.AreEqual("Added", auditEntity.Action);
            Assert.AreEqual(AuditStatus.Success.ToString(), auditEntity.Status);
            Assert.IsGreaterThanOrEqualTo(auditWindowStartUtc, auditEntity.RecordedUtc);
        }
        catch
        {
            foreach (var resourceName in new[] { "taskflowfunctions", "taskflowdb", "TableStorage1", "ServiceBus1" })
            {
                await AspireTestHost.DumpResourceDiagnosticsAsync(resourceName, ct);
            }

            throw;
        }
    }

    /// <summary>Verifies the Functions host health endpoint becomes reachable before issuing the integration request.</summary>
    private static async Task WaitForFunctionReadyAsync(HttpClient client, CancellationToken ct)
    {
        Exception? lastException = null;

        try
        {
            while (true)
            {
                try
                {
                    using var response = await client.GetAsync("/api/health", ct);
                    if (response.IsSuccessStatusCode)
                        return;

                    var responseBody = await response.Content.ReadAsStringAsync(ct);
                    lastException = new InvalidOperationException(
                        $"Function health endpoint returned {(int)response.StatusCode} ({response.ReasonPhrase}). "
                        + $"Body: {Truncate(responseBody)}");
                }
                catch (HttpRequestException ex)
                {
                    lastException = ex;
                }

                await Task.Delay(TimeSpan.FromSeconds(1), ct);
            }
        }
        catch (OperationCanceledException ex) when (ct.IsCancellationRequested)
        {
            throw new OperationCanceledException(
                $"Function host did not become ready before the shared startup deadline. Last error: {lastException?.Message ?? "unknown"}",
                lastException ?? ex,
                ct);
        }
    }

    private static string Truncate(string value) => value.Length <= 2_000 ? value : value[..2_000] + "...";

    /// <summary>Verifies post create category with retry behavior and protects the expected test contract.</summary>
    private static async Task<HttpResponseMessage> PostCreateCategoryWithRetryAsync(HttpClient client, object request, CancellationToken ct)
    {
        var deadline = DateTimeOffset.UtcNow.AddSeconds(180);
        HttpStatusCode? lastStatusCode = null;
        string? lastBody = null;
        Exception? lastException = null;

        while (DateTimeOffset.UtcNow < deadline)
        {
            try
            {
                var response = await client.PostAsJsonAsync("/api/v1/categories", request, ct);
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
            catch (TaskCanceledException ex)
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

    /// <summary>Verifies wait for audit entity behavior and protects the expected test contract.</summary>
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
