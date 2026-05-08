using Aspire.Hosting.Testing;
using Azure.Data.Tables;
using EF.Common.Contracts;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using TaskFlow.Infrastructure.Storage;

namespace Test.Integration;

/// <summary>
/// Validates <c>AuditLogRepository.AppendAsync</c> against real Azurite Table Storage: partition key,
/// row key shape (<c>..._{Id:N}</c>), and round-trip of audit metadata.
/// Aspire tier by reuse: only Azurite is exercised — no API, no Function — but the test piggybacks on
/// the shared <c>AspireTestHost</c> <c>TableStorage1</c> resource instead of starting its own Azurite
/// container. A dedicated Testcontainers Azurite fixture would also work; reusing Aspire avoids a second
/// container per test run.
/// </summary>
[TestClass]
[TestCategory("Integration")]
[DoNotParallelize]
public class AuditLogRepositoryAzuriteTests
{
    [TestMethod]
    [Timeout(300000)]
    public async Task Given_AuditEntry_When_AppendAsyncToAzurite_Then_TableEntityPersistedWithExpectedKeys()
    {
        var ct = CancellationToken.None;

        await AspireTestHost.WaitForResourceHealthyAsync("TableStorage1", ct);

        var connectionString = await AspireTestHost.AspireApp!.GetConnectionStringAsync("TableStorage1", ct)
            .AsTask()
            .WaitAsync(AspireTestHost.DefaultTimeout, ct);
        Assert.IsFalse(string.IsNullOrWhiteSpace(connectionString));

        var tableName = $"audit{Guid.NewGuid():N}"[..31];
        var tableServiceClient = new TableServiceClient(connectionString);
        var repository = new AuditLogRepository(
            new TestTableServiceClientFactory(tableServiceClient),
            Options.Create(new AuditLogStorageSettings
            {
                TableName = tableName,
                NullTenantPartitionKey = "_system"
            }),
            NullLogger<AuditLogRepository>.Instance);

        var tenantId = Guid.NewGuid();
        var entry = new AuditEntry<string, Guid>
        {
            Id = Guid.NewGuid(),
            AuditId = "integration-user",
            TenantId = tenantId,
            EntityType = "TaskItem",
            EntityKey = Guid.NewGuid().ToString(),
            Status = AuditStatus.Success,
            Action = "Create",
            StartTime = TimeSpan.FromMilliseconds(25),
            ElapsedTime = TimeSpan.FromMilliseconds(7),
            Metadata = "{\"source\":\"azurite-test\"}"
        };

        try
        {
            await repository.AppendAsync(entry, ct);

            var tableClient = tableServiceClient.GetTableClient(tableName);
            var persisted = await ReadSingleEntityAsync(tableClient, tenantId.ToString());

            Assert.IsNotNull(persisted);
            Assert.AreEqual(tenantId.ToString(), persisted.PartitionKey);
            Assert.IsTrue(persisted.RowKey.EndsWith($"_{entry.Id:N}", StringComparison.Ordinal));
            Assert.AreEqual(entry.AuditId, persisted.AuditId);
            Assert.AreEqual(tenantId.ToString(), persisted.TenantId);
            Assert.AreEqual(entry.EntityType, persisted.EntityType);
            Assert.AreEqual(entry.EntityKey, persisted.EntityKey);
            Assert.AreEqual(entry.Action, persisted.Action);
            Assert.AreEqual(entry.Status.ToString(), persisted.Status);
            Assert.AreEqual(entry.Metadata, persisted.Metadata);
        }
        finally
        {
            await tableServiceClient.DeleteTableAsync(tableName);
        }
    }

    private static async Task<AuditLogTableEntity> ReadSingleEntityAsync(TableClient tableClient, string partitionKey)
    {
        await foreach (var entity in tableClient.QueryAsync<AuditLogTableEntity>(
            entity => entity.PartitionKey == partitionKey))
        {
            return entity;
        }

        Assert.Fail("Expected an audit entity to be written to Azurite.");
        throw new InvalidOperationException("Unreachable");
    }

    private sealed class TestTableServiceClientFactory(TableServiceClient client) : IAzureClientFactory<TableServiceClient>
    {
        public TableServiceClient CreateClient(string name) => client;
    }
}