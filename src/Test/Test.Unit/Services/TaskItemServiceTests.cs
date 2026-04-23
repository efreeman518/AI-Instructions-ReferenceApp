using EF.Domain.Contracts;
using EF.Data.Contracts;
using Moq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using EF.Common.Contracts;
using TaskFlow.Application.Contracts;
using TaskFlow.Application.Contracts.Messaging;
using TaskFlow.Application.Contracts.Repositories;
using TaskFlow.Application.Models;
using TaskFlow.Application.Services;
using TaskFlow.Domain.Model;
using TaskFlow.Domain.Shared.Enums;
using Test.Support;
using Test.Support.Builders;

namespace Test.Unit.Services;

[TestClass]
public class TaskItemServiceTests
{
    private readonly Mock<ITaskItemRepositoryTrxn> _repoTrxnMock = new();
    private readonly Mock<ITaskItemRepositoryQuery> _repoQueryMock = new();
    private readonly Mock<IRequestContext<string, Guid?>> _requestContextMock = new();
    private readonly Mock<ITenantBoundaryValidator> _tenantBoundaryValidatorMock = new();
    private readonly Mock<IEntityCacheProvider> _cacheMock = new();
    private readonly Mock<IIntegrationEventPublisher> _eventPublisherMock = new();

    [TestInitialize]
    public void Setup()
    {
        _requestContextMock.Setup(x => x.TenantId).Returns(TestConstants.TenantId);
        _requestContextMock.Setup(x => x.Roles).Returns(new List<string>());
        _tenantBoundaryValidatorMock
            .Setup(x => x.EnsureTenantBoundary(It.IsAny<ILogger>(), It.IsAny<Guid?>(), It.IsAny<IReadOnlyCollection<string>>(), It.IsAny<Guid?>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Guid?>()))
            .Returns(Result.Success());
        _tenantBoundaryValidatorMock
            .Setup(x => x.PreventTenantChange(It.IsAny<ILogger>(), It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<string>(), It.IsAny<Guid>()))
            .Returns(Result.Success());
    }

    private TaskItemService CreateService() => new(
        NullLogger<TaskItemService>.Instance,
        _requestContextMock.Object,
        _repoTrxnMock.Object,
        _repoQueryMock.Object,
        _tenantBoundaryValidatorMock.Object,
        _cacheMock.Object,
        _eventPublisherMock.Object);

    [TestMethod]
    [TestCategory("Unit")]
    public async Task Given_ValidDto_When_CreateAsync_Then_ReturnsSuccess()
    {
        _repoTrxnMock.Setup(r => r.Create(ref It.Ref<TaskItem>.IsAny));
        _repoTrxnMock.Setup(r => r.UpdateFromDto(It.IsAny<TaskItem>(), It.IsAny<TaskItemDto>(), It.IsAny<RelatedDeleteBehavior>()))
            .Returns((TaskItem e, TaskItemDto _, RelatedDeleteBehavior __) => DomainResult<TaskItem>.Success(e));
        _repoTrxnMock.Setup(r => r.SaveChangesAsync(It.IsAny<OptimisticConcurrencyWinner>(), It.IsAny<CancellationToken>())).ReturnsAsync(0);

        var dto = new TaskItemDto { Title = "Test Task", Priority = Priority.Medium };
        var result = await CreateService().CreateAsync(new DefaultRequest<TaskItemDto> { Item = dto });

        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual("Test Task", result.Value!.Item!.Title);
        Assert.AreEqual(TaskItemStatus.Open, result.Value.Item.Status);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task Given_InvalidDto_When_CreateAsync_Then_ReturnsFailure()
    {
        var dto = new TaskItemDto { Title = "" };
        var result = await CreateService().CreateAsync(new DefaultRequest<TaskItemDto> { Item = dto });

        Assert.IsTrue(result.IsFailure);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task Given_ExistingEntity_When_GetAsync_Then_ReturnsMappedDto()
    {
        var entity = new TaskItemBuilder().Build();
        _repoQueryMock.Setup(r => r.GetTaskItemAsync(entity.Id, It.IsAny<CancellationToken>())).ReturnsAsync(entity);

        var result = await CreateService().GetAsync(entity.Id);

        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual(entity.Title, result.Value!.Item!.Title);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task Given_NonExistentId_When_GetAsync_Then_ReturnsNone()
    {
        _repoQueryMock.Setup(r => r.GetTaskItemAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((TaskItem?)null);

        var result = await CreateService().GetAsync(Guid.NewGuid());

        Assert.IsTrue(result.IsNone);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task Given_ExistingEntity_When_UpdateAsync_Then_ReturnsSuccess()
    {
        var entity = new TaskItemBuilder().Build();
        _repoTrxnMock.Setup(r => r.GetTaskItemAsync(entity.Id, It.IsAny<bool>(), It.IsAny<CancellationToken>())).ReturnsAsync(entity);
        _repoTrxnMock.Setup(r => r.UpdateFromDto(It.IsAny<TaskItem>(), It.IsAny<TaskItemDto>(), It.IsAny<RelatedDeleteBehavior>()))
            .Returns((TaskItem e, TaskItemDto _, RelatedDeleteBehavior __) => DomainResult<TaskItem>.Success(e));
        _repoTrxnMock.Setup(r => r.SaveChangesAsync(It.IsAny<OptimisticConcurrencyWinner>(), It.IsAny<CancellationToken>())).ReturnsAsync(0);

        var dto = new TaskItemDto { Id = entity.Id, Title = "Updated Title", Status = TaskItemStatus.Open };
        var result = await CreateService().UpdateAsync(new DefaultRequest<TaskItemDto> { Item = dto });

        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual("Updated Title", result.Value!.Item!.Title);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task Given_ExistingEntity_When_UpdateWithStatusTransition_Then_StatusUpdated()
    {
        var entity = new TaskItemBuilder().Build(); // Status = Open
        _repoTrxnMock.Setup(r => r.GetTaskItemAsync(entity.Id, It.IsAny<bool>(), It.IsAny<CancellationToken>())).ReturnsAsync(entity);
        _repoTrxnMock.Setup(r => r.UpdateFromDto(It.IsAny<TaskItem>(), It.IsAny<TaskItemDto>(), It.IsAny<RelatedDeleteBehavior>()))
            .Returns((TaskItem e, TaskItemDto _, RelatedDeleteBehavior __) => DomainResult<TaskItem>.Success(e));
        _repoTrxnMock.Setup(r => r.SaveChangesAsync(It.IsAny<OptimisticConcurrencyWinner>(), It.IsAny<CancellationToken>())).ReturnsAsync(0);

        var dto = new TaskItemDto { Id = entity.Id, Title = entity.Title, Status = TaskItemStatus.InProgress };
        var result = await CreateService().UpdateAsync(new DefaultRequest<TaskItemDto> { Item = dto });

        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual(TaskItemStatus.InProgress, result.Value!.Item!.Status);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task Given_ExistingEntity_When_UpdateWithInvalidTransition_Then_ReturnsFailure()
    {
        var entity = new TaskItemBuilder().Build(); // Status = Open
        _repoTrxnMock.Setup(r => r.GetTaskItemAsync(entity.Id, It.IsAny<bool>(), It.IsAny<CancellationToken>())).ReturnsAsync(entity);

        var dto = new TaskItemDto { Id = entity.Id, Title = entity.Title, Status = TaskItemStatus.Completed };
        var result = await CreateService().UpdateAsync(new DefaultRequest<TaskItemDto> { Item = dto });

        Assert.IsTrue(result.IsFailure);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task Given_NonExistentId_When_UpdateAsync_Then_ReturnsNullItem()
    {
        _repoTrxnMock.Setup(r => r.GetTaskItemAsync(It.IsAny<Guid>(), It.IsAny<bool>(), It.IsAny<CancellationToken>())).ReturnsAsync((TaskItem?)null);

        var dto = new TaskItemDto { Id = Guid.NewGuid(), Title = "Updated" };
        var result = await CreateService().UpdateAsync(new DefaultRequest<TaskItemDto> { Item = dto });

        Assert.IsTrue(result.IsSuccess);
        Assert.IsNull(result.Value?.Item);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task Given_ExistingEntity_When_DeleteAsync_Then_ReturnsSuccess()
    {
        var entity = new TaskItemBuilder().Build();
        _repoTrxnMock.Setup(r => r.GetTaskItemAsync(entity.Id, It.IsAny<bool>(), It.IsAny<CancellationToken>())).ReturnsAsync(entity);
        _repoTrxnMock.Setup(r => r.SaveChangesAsync(It.IsAny<OptimisticConcurrencyWinner>(), It.IsAny<CancellationToken>())).ReturnsAsync(0);

        var result = await CreateService().DeleteAsync(entity.Id);

        Assert.IsTrue(result.IsSuccess);
        _repoTrxnMock.Verify(r => r.Delete(entity), Times.Once);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task Given_NonExistentId_When_DeleteAsync_Then_ReturnsSuccessIdempotent()
    {
        _repoTrxnMock.Setup(r => r.GetTaskItemAsync(It.IsAny<Guid>(), It.IsAny<bool>(), It.IsAny<CancellationToken>())).ReturnsAsync((TaskItem?)null);

        var result = await CreateService().DeleteAsync(Guid.NewGuid());

        Assert.IsTrue(result.IsSuccess);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task Given_SearchRequest_When_SearchAsync_Then_ReturnsPagedResponse()
    {
        var dto = new TaskItemDto { Title = "Test" };
        var pagedResponse = new PagedResponse<TaskItemDto> { Data = [dto], Total = 1, PageSize = 10, PageIndex = 0 };
        _repoQueryMock.Setup(r => r.SearchTaskItemsAsync(It.IsAny<SearchRequest<TaskItemSearchFilter>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedResponse);

        var request = new SearchRequest<TaskItemSearchFilter> { PageSize = 10, PageIndex = 0 };
        var response = await CreateService().SearchAsync(request);

        Assert.AreEqual(1, response.Total);
    }
}
