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

[TestClass]
public class CategoryServiceTests
{
    private readonly Mock<ICategoryRepositoryTrxn> _repoTrxnMock = new();
    private readonly Mock<ICategoryRepositoryQuery> _repoQueryMock = new();
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

    private CategoryService CreateService() => new(
        NullLogger<CategoryService>.Instance,
        _requestContextMock.Object,
        _repoTrxnMock.Object,
        _repoQueryMock.Object,
        _tenantBoundaryValidatorMock.Object,
        _cacheMock.Object);

    [TestMethod]
    [TestCategory("Unit")]
    public async Task Given_ValidDto_When_CreateAsync_Then_ReturnsSuccess()
    {
        _repoTrxnMock.Setup(r => r.Create(ref It.Ref<Category>.IsAny));
        _repoTrxnMock.Setup(r => r.SaveChangesAsync(It.IsAny<OptimisticConcurrencyWinner>(), It.IsAny<CancellationToken>())).ReturnsAsync(0);

        var dto = new CategoryDto { Name = "Test Category", Description = "Desc" };
        var result = await CreateService().CreateAsync(new DefaultRequest<CategoryDto> { Item = dto });

        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual("Test Category", result.Value!.Item!.Name);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task Given_InvalidDto_When_CreateAsync_Then_ReturnsFailure()
    {
        var dto = new CategoryDto { Name = "" };
        var result = await CreateService().CreateAsync(new DefaultRequest<CategoryDto> { Item = dto });

        Assert.IsTrue(result.IsFailure);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task Given_ExistingEntity_When_GetAsync_Then_ReturnsMappedDto()
    {
        var entity = new CategoryBuilder().Build();
        _repoQueryMock.Setup(r => r.GetCategoryAsync(entity.Id, It.IsAny<CancellationToken>())).ReturnsAsync(entity);

        var result = await CreateService().GetAsync(entity.Id);

        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual(entity.Name, result.Value!.Item!.Name);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task Given_NonExistentId_When_GetAsync_Then_ReturnsNone()
    {
        _repoQueryMock.Setup(r => r.GetCategoryAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((Category?)null);

        var result = await CreateService().GetAsync(Guid.NewGuid());

        Assert.IsTrue(result.IsNone);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task Given_ExistingEntity_When_UpdateAsync_Then_ReturnsSuccess()
    {
        var entity = new CategoryBuilder().Build();
        _repoTrxnMock.Setup(r => r.GetCategoryAsync(entity.Id, It.IsAny<CancellationToken>())).ReturnsAsync(entity);
        _repoTrxnMock.Setup(r => r.SaveChangesAsync(It.IsAny<OptimisticConcurrencyWinner>(), It.IsAny<CancellationToken>())).ReturnsAsync(0);

        var dto = new CategoryDto { Id = entity.Id, Name = "Updated Name" };
        var result = await CreateService().UpdateAsync(new DefaultRequest<CategoryDto> { Item = dto });

        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual("Updated Name", result.Value!.Item!.Name);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task Given_NonExistentId_When_UpdateAsync_Then_ReturnsNullItem()
    {
        _repoTrxnMock.Setup(r => r.GetCategoryAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((Category?)null);

        var dto = new CategoryDto { Id = Guid.NewGuid(), Name = "Updated" };
        var result = await CreateService().UpdateAsync(new DefaultRequest<CategoryDto> { Item = dto });

        Assert.IsTrue(result.IsSuccess);
        Assert.IsNull(result.Value?.Item);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task Given_ExistingEntity_When_DeleteAsync_Then_ReturnsSuccess()
    {
        var entity = new CategoryBuilder().Build();
        _repoTrxnMock.Setup(r => r.GetCategoryAsync(entity.Id, It.IsAny<CancellationToken>())).ReturnsAsync(entity);
        _repoTrxnMock.Setup(r => r.SaveChangesAsync(It.IsAny<OptimisticConcurrencyWinner>(), It.IsAny<CancellationToken>())).ReturnsAsync(0);

        var result = await CreateService().DeleteAsync(entity.Id);

        Assert.IsTrue(result.IsSuccess);
        _repoTrxnMock.Verify(r => r.Delete(entity), Times.Once);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task Given_NonExistentId_When_DeleteAsync_Then_ReturnsSuccessIdempotent()
    {
        _repoTrxnMock.Setup(r => r.GetCategoryAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((Category?)null);

        var result = await CreateService().DeleteAsync(Guid.NewGuid());

        Assert.IsTrue(result.IsSuccess);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task Given_SearchRequest_When_SearchAsync_Then_ReturnsPagedResponse()
    {
        var entities = new List<Category> { new CategoryBuilder().Build(), new CategoryBuilder().WithName("Second").Build() };
        var pagedResponse = new PagedResponse<Category> { Data = entities, Total = 2, PageSize = 10, PageIndex = 0 };
        _repoQueryMock.Setup(r => r.SearchCategoriesAsync(It.IsAny<SearchRequest<CategorySearchFilter>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedResponse);

        var request = new SearchRequest<CategorySearchFilter> { PageSize = 10, PageIndex = 0 };
        var response = await CreateService().SearchAsync(request);

        Assert.AreEqual(2, response.Total);
        Assert.HasCount(2, response.Data);
    }
}
