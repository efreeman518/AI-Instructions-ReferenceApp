using Azure.Data.Tables;
using EF.Common.Contracts;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using TaskFlow.Infrastructure.Storage;
using Test.Integration.Infrastructure;

namespace Test.Integration;

/// <summary>
/// Validates <c>AuditLogRepository.AppendAsync</c> against real Azurite Table Storage: partition key,
/// row key shape (<c>..._{Id:N}</c>), and round-trip of audit metadata.
/// Component tier: exercises only Azurite via a standalone <c>AzuriteContainerFixture</c> (started by
/// <c>IntegrationTestSetup</c>) - no API, no Function, no Aspire graph.
/// </summary>
[TestClass]
[TestCategory("Integration")]
[DoNotParallelize]
public class AuditLogRepositoryAzuriteTests
{
    /// <summary>Classifies Docker unavailability separately from an Azurite startup failure.</summary>
    [TestInitialize]
    public void TestSetup()
    {
        IntegrationTestSetup.AssertAvailable("Azurite", AzuriteContainerFixture.StartupError);
    }

    /// <summary>Verifies that given audit entry, when append to azurite, then table entity persisted with expected keys.</summary>
    [TestMethod]
    [Timeout(300000, CooperativeCancellation = true)]
    public async Task Given_AuditEntry_When_AppendAsyncToAzurite_Then_TableEntityPersistedWithExpectedKeys()
    {
        var ct = CancellationToken.None;

        var connectionString = AzuriteContainerFixture.ConnectionString;
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
            await tableServiceClient.DeleteTableAsync(tableName, TestContext.CancellationToken);
        }
    }

    /// <summary>Verifies read single entity behavior and protects the expected test contract.</summary>
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

    /// <summary>Builds test table service client test hosts with deterministic dependencies for repeatable test execution.</summary>
    private sealed class TestTableServiceClientFactory(TableServiceClient client) : IAzureClientFactory<TableServiceClient>
    {
        /// <summary>Creates client used by the surrounding test cases.</summary>
        public TableServiceClient CreateClient(string name) => client;
    }

    public TestContext TestContext { get; set; } = null!;
}
