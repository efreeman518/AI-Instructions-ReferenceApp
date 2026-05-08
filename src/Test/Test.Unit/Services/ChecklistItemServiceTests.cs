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
/// Validates <see cref="TaskFlow.Application.Services.ChecklistItemService"/> orchestration with mocked
/// dependencies: CRUD success/failure paths and the IsCompleted flag flow on update.
/// Pure-unit tier (Moq only).
/// </summary>
[TestClass]
public class ChecklistItemServiceTests
{
    private readonly Mock<IChecklistItemRepositoryTrxn> _repoTrxnMock = new();
    private readonly Mock<IChecklistItemRepositoryQuery> _repoQueryMock = new();
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
        _tenantBoundaryValidatorMock
            .Setup(x => x.PreventTenantChange(It.IsAny<ILogger>(), It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<string>(), It.IsAny<Guid>()))
            .Returns(Result.Success());
    }

    private ChecklistItemService CreateService() => new(
        NullLogger<ChecklistItemService>.Instance,
        _requestContextMock.Object,
        _repoTrxnMock.Object,
        _repoQueryMock.Object,
        _tenantBoundaryValidatorMock.Object,
        _cacheMock.Object);

    [TestMethod]
    [TestCategory("Unit")]
    public async Task Given_ValidDto_When_CreateAsync_Then_ReturnsSuccess()
    {
        _repoTrxnMock.Setup(r => r.Create(ref It.Ref<ChecklistItem>.IsAny));
        _repoTrxnMock.Setup(r => r.SaveChangesAsync(It.IsAny<OptimisticConcurrencyWinner>(), It.IsAny<CancellationToken>())).ReturnsAsync(0);

        var dto = new ChecklistItemDto { Title = "Step 1", TaskItemId = Guid.NewGuid(), SortOrder = 0 };
        var result = await CreateService().CreateAsync(new DefaultRequest<ChecklistItemDto> { Item = dto });

        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual("Step 1", result.Value!.Item!.Title);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task Given_InvalidDto_When_CreateAsync_Then_ReturnsFailure()
    {
        var dto = new ChecklistItemDto { Title = "", TaskItemId = Guid.Empty };
        var result = await CreateService().CreateAsync(new DefaultRequest<ChecklistItemDto> { Item = dto });

        Assert.IsTrue(result.IsFailure);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task Given_ExistingEntity_When_GetAsync_Then_ReturnsMappedDto()
    {
        var entity = new ChecklistItemBuilder().Build();
        _repoQueryMock.Setup(r => r.GetChecklistItemAsync(entity.Id, It.IsAny<CancellationToken>())).ReturnsAsync(entity);

        var result = await CreateService().GetAsync(entity.Id);

        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual(entity.Title, result.Value!.Item!.Title);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task Given_NonExistentId_When_GetAsync_Then_ReturnsNone()
    {
        _repoQueryMock.Setup(r => r.GetChecklistItemAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((ChecklistItem?)null);

        var result = await CreateService().GetAsync(Guid.NewGuid());

        Assert.IsTrue(result.IsNone);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task Given_ExistingEntity_When_UpdateAsync_Then_ReturnsSuccess()
    {
        var entity = new ChecklistItemBuilder().Build();
        _repoTrxnMock.Setup(r => r.GetChecklistItemAsync(entity.Id, It.IsAny<CancellationToken>())).ReturnsAsync(entity);
        _repoTrxnMock.Setup(r => r.SaveChangesAsync(It.IsAny<OptimisticConcurrencyWinner>(), It.IsAny<CancellationToken>())).ReturnsAsync(0);

        var dto = new ChecklistItemDto { Id = entity.Id, Title = "Updated Step", TaskItemId = entity.TaskItemId, IsCompleted = true };
        var result = await CreateService().UpdateAsync(new DefaultRequest<ChecklistItemDto> { Item = dto });

        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual("Updated Step", result.Value!.Item!.Title);
        Assert.IsTrue(result.Value.Item.IsCompleted);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task Given_NonExistentId_When_UpdateAsync_Then_ReturnsNullItem()
    {
        _repoTrxnMock.Setup(r => r.GetChecklistItemAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((ChecklistItem?)null);

        var dto = new ChecklistItemDto { Id = Guid.NewGuid(), Title = "Updated" };
        var result = await CreateService().UpdateAsync(new DefaultRequest<ChecklistItemDto> { Item = dto });

        Assert.IsTrue(result.IsSuccess);
        Assert.IsNull(result.Value?.Item);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task Given_ExistingEntity_When_DeleteAsync_Then_ReturnsSuccess()
    {
        var entity = new ChecklistItemBuilder().Build();
        _repoTrxnMock.Setup(r => r.GetChecklistItemAsync(entity.Id, It.IsAny<CancellationToken>())).ReturnsAsync(entity);
        _repoTrxnMock.Setup(r => r.SaveChangesAsync(It.IsAny<OptimisticConcurrencyWinner>(), It.IsAny<CancellationToken>())).ReturnsAsync(0);

        var result = await CreateService().DeleteAsync(entity.Id);

        Assert.IsTrue(result.IsSuccess);
        _repoTrxnMock.Verify(r => r.Delete(entity), Times.Once);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task Given_NonExistentId_When_DeleteAsync_Then_ReturnsSuccessIdempotent()
    {
        _repoTrxnMock.Setup(r => r.GetChecklistItemAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((ChecklistItem?)null);

        var result = await CreateService().DeleteAsync(Guid.NewGuid());

        Assert.IsTrue(result.IsSuccess);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task Given_SearchRequest_When_SearchAsync_Then_ReturnsPagedResponse()
    {
        var dtos = new List<ChecklistItemDto> { new() { Title = "Test" } };
        var pagedResponse = new PagedResponse<ChecklistItemDto> { Data = dtos, Total = 1, PageSize = 10, PageIndex = 0 };
        _repoQueryMock.Setup(r => r.SearchChecklistItemsAsync(It.IsAny<SearchRequest<ChecklistItemSearchFilter>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedResponse);

        var request = new SearchRequest<ChecklistItemSearchFilter> { PageSize = 10, PageIndex = 0 };
        var response = await CreateService().SearchAsync(request);

        Assert.AreEqual(1, response.Total);
    }
}
