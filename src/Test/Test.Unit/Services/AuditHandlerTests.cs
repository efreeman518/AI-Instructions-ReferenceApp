using EF.Common.Contracts;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using TaskFlow.Application.Contracts.Storage;
using TaskFlow.Application.MessageHandlers;

namespace Test.Unit.Services;

/// <summary>
/// Validates <see cref="TaskFlow.Application.MessageHandlers.AuditHandler"/> forwards the inbound
/// <c>AuditEntry</c> to <c>IAuditLogRepository.AppendAsync</c> for both <c>Guid</c> and nullable-<c>Guid</c>
/// tenant generic shapes, preserving Id/TenantId/Action.
/// Pure-unit tier (Moq only): the repository is substituted — Azurite-backed persistence is covered by
/// <c>AuditLogRepositoryAzuriteTests</c> in Test.Integration.
/// </summary>
[TestClass]
public class AuditHandlerTests
{
    private readonly Mock<IAuditLogRepository> _auditLogRepositoryMock = new();

    private AuditHandler CreateHandler() => new(
        NullLogger<AuditHandler>.Instance,
        _auditLogRepositoryMock.Object);

    [TestMethod]
    [TestCategory("Unit")]
    public async Task HandleAsync_WithGuidTenant_PersistsAuditEntry()
    {
        var message = new AuditEntry<string, Guid>
        {
            Id = Guid.NewGuid(),
            AuditId = "user-1",
            TenantId = Guid.NewGuid(),
            EntityType = "TaskItem",
            EntityKey = Guid.NewGuid().ToString(),
            Status = AuditStatus.Success,
            Action = "Create",
            StartTime = TimeSpan.FromMilliseconds(10),
            ElapsedTime = TimeSpan.FromMilliseconds(4),
            Metadata = "{}"
        };

        await CreateHandler().HandleAsync(message, CancellationToken.None);

        _auditLogRepositoryMock.Verify(
            repository => repository.AppendAsync<Guid>(
                It.Is<AuditEntry<string, Guid>>(entry =>
                    entry.Id == message.Id &&
                    entry.TenantId == message.TenantId &&
                    entry.Action == message.Action),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task HandleAsync_WithNullableGuidTenant_PersistsAuditEntry()
    {
        var message = new AuditEntry<string, Guid?>
        {
            Id = Guid.NewGuid(),
            AuditId = "user-2",
            TenantId = null,
            EntityType = "Category",
            EntityKey = Guid.NewGuid().ToString(),
            Status = AuditStatus.Success,
            Action = "Update",
            StartTime = TimeSpan.FromMilliseconds(12),
            ElapsedTime = TimeSpan.FromMilliseconds(6),
            Metadata = "{\"changed\":true}"
        };

        await CreateHandler().HandleAsync(message, CancellationToken.None);

        _auditLogRepositoryMock.Verify(
            repository => repository.AppendAsync<Guid?>(
                It.Is<AuditEntry<string, Guid?>>(entry =>
                    entry.Id == message.Id &&
                    entry.TenantId == null &&
                    entry.Action == message.Action),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}