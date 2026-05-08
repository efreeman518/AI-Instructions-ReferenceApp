using Moq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using EF.Common.Contracts;
using EF.Data.Contracts;
using TaskFlow.Application.Contracts;
using TaskFlow.Application.Contracts.Repositories;
using TaskFlow.Application.Models;
using TaskFlow.Application.Services;
using TaskFlow.Domain.Model;
using Test.Support;
using Test.Support.Builders;

namespace Test.Unit.Services;

/// <summary>
/// Validates <see cref="TaskFlow.Application.Services.TaskItemTagService"/> orchestration over the
/// many-to-many bridge entity with mocked dependencies: create/get/delete and idempotent delete.
/// Pure-unit tier (Moq only).
/// </summary>
[TestClass]
public class TaskItemTagServiceTests
{
    private readonly Mock<ITaskItemTagRepositoryTrxn> _repoTrxnMock = new();
    private readonly Mock<ITaskItemTagRepositoryQuery> _repoQueryMock = new();
    private readonly Mock<IRequestContext<string, Guid?>> _requestContextMock = new();
    private readonly Mock<ITenantBoundaryValidator> _tenantBoundaryValidatorMock = new();
    private readonly Mock<IEntityCacheProvider> _cacheMock = new();

    [TestInitialize]
    public void Setup()
    {
        _requestContextMock.Setup(x => x.TenantId).Returns(TestConstants.TenantId);
        _requestContextMock.Setup(x => x.Roles).Returns(new List<string>());
        _tenantBoundaryValidatorMock
            .Setup(x => x.EnsureTenantBoundary(It.IsAny<ILogger>(), It.IsAny<Guid?>(), It.IsAny<IReadOnlyCollection<string>>(), It.IsAny<Guid?>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Guid?>()))
            .Returns(Result.Success());
    }

    private TaskItemTagService CreateService() => new(
        NullLogger<TaskItemTagService>.Instance,
        _requestContextMock.Object,
        _repoTrxnMock.Object,
        _repoQueryMock.Object,
        _tenantBoundaryValidatorMock.Object,
        _cacheMock.Object);

    [TestMethod]
    [TestCategory("Unit")]
    public async Task Given_ValidDto_When_CreateAsync_Then_ReturnsSuccess()
    {
        _repoTrxnMock.Setup(r => r.Create(ref It.Ref<TaskItemTag>.IsAny));
        _repoTrxnMock.Setup(r => r.SaveChangesAsync(It.IsAny<OptimisticConcurrencyWinner>(), It.IsAny<CancellationToken>())).ReturnsAsync(0);

        var dto = new TaskItemTagDto { TaskItemId = Guid.NewGuid(), TagId = Guid.NewGuid() };
        var result = await CreateService().CreateAsync(new DefaultRequest<TaskItemTagDto> { Item = dto });

        Assert.IsTrue(result.IsSuccess);
        Assert.AreNotEqual(Guid.Empty, result.Value!.Item!.TaskItemId);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task Given_InvalidDto_When_CreateAsync_Then_ReturnsFailure()
    {
        var dto = new TaskItemTagDto { TaskItemId = Guid.Empty, TagId = Guid.Empty };
        var result = await CreateService().CreateAsync(new DefaultRequest<TaskItemTagDto> { Item = dto });

        Assert.IsTrue(result.IsFailure);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task Given_ExistingEntity_When_GetAsync_Then_ReturnsMappedDto()
    {
        var entity = new TaskItemTagBuilder().Build();
        _repoQueryMock.Setup(r => r.GetTaskItemTagAsync(entity.Id, It.IsAny<CancellationToken>())).ReturnsAsync(entity);

        var result = await CreateService().GetAsync(entity.Id);

        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual(entity.TaskItemId, result.Value!.Item!.TaskItemId);
        Assert.AreEqual(entity.TagId, result.Value.Item.TagId);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task Given_NonExistentId_When_GetAsync_Then_ReturnsNone()
    {
        _repoQueryMock.Setup(r => r.GetTaskItemTagAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((TaskItemTag?)null);

        var result = await CreateService().GetAsync(Guid.NewGuid());

        Assert.IsTrue(result.IsNone);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task Given_ExistingEntity_When_DeleteAsync_Then_ReturnsSuccess()
    {
        var entity = new TaskItemTagBuilder().Build();
        _repoTrxnMock.Setup(r => r.GetTaskItemTagAsync(entity.Id, It.IsAny<CancellationToken>())).ReturnsAsync(entity);
        _repoTrxnMock.Setup(r => r.SaveChangesAsync(It.IsAny<OptimisticConcurrencyWinner>(), It.IsAny<CancellationToken>())).ReturnsAsync(0);

        var result = await CreateService().DeleteAsync(entity.Id);

        Assert.IsTrue(result.IsSuccess);
        _repoTrxnMock.Verify(r => r.Delete(entity), Times.Once);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task Given_NonExistentId_When_DeleteAsync_Then_ReturnsSuccessIdempotent()
    {
        _repoTrxnMock.Setup(r => r.GetTaskItemTagAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((TaskItemTag?)null);

        var result = await CreateService().DeleteAsync(Guid.NewGuid());

        Assert.IsTrue(result.IsSuccess);
    }
}
