using Aspire.Hosting.Testing;
using Azure.Data.Tables;
using EF.Common.Contracts;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using TaskFlow.Infrastructure.Storage;

namespace Test.Integration;

[TestClass]
[TestCategory("Integration")]
[DoNotParallelize]
public class AuditLogRepositoryAzuriteTests
{
    [TestMethod]
    [Timeout(300000)]
    public async Task Given_AuditEntry_When_AppendAsyncToAzurite_Then_TableEntityPersistedWithExpectedKeys()
    {
        var connectionString = await DatabaseFixture.AspireApp!.GetConnectionStringAsync("TableStorage1");
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
            await repository.AppendAsync(entry, CancellationToken.None);

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