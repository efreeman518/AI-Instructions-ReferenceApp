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
using TaskFlow.Domain.Shared.Enums;
using Test.Support;
using Test.Support.Builders;

namespace Test.Unit.Services;

[TestClass]
public class AttachmentServiceTests
{
    private readonly Mock<IAttachmentRepositoryTrxn> _repoTrxnMock = new();
    private readonly Mock<IAttachmentRepositoryQuery> _repoQueryMock = new();
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

    private AttachmentService CreateService() => new(
        NullLogger<AttachmentService>.Instance,
        _requestContextMock.Object,
        _repoTrxnMock.Object,
        _repoQueryMock.Object,
        _tenantBoundaryValidatorMock.Object,
        _cacheMock.Object);

    [TestMethod]
    [TestCategory("Unit")]
    public async Task Given_ValidDto_When_CreateAsync_Then_ReturnsSuccess()
    {
        _repoTrxnMock.Setup(r => r.Create(ref It.Ref<Attachment>.IsAny));
        _repoTrxnMock.Setup(r => r.SaveChangesAsync(It.IsAny<OptimisticConcurrencyWinner>(), It.IsAny<CancellationToken>())).ReturnsAsync(0);

        var dto = new AttachmentDto
        {
            FileName = "doc.pdf",
            ContentType = "application/pdf",
            FileSizeBytes = 1024,
            StorageUri = "https://storage.example.com/doc.pdf",
            OwnerType = AttachmentOwnerType.TaskItem,
            OwnerId = Guid.NewGuid()
        };
        var result = await CreateService().CreateAsync(new DefaultRequest<AttachmentDto> { Item = dto });

        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual("doc.pdf", result.Value!.Item!.FileName);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task Given_InvalidDto_When_CreateAsync_Then_ReturnsFailure()
    {
        var dto = new AttachmentDto { FileName = "", ContentType = "", FileSizeBytes = 0, StorageUri = "", OwnerId = Guid.Empty };
        var result = await CreateService().CreateAsync(new DefaultRequest<AttachmentDto> { Item = dto });

        Assert.IsTrue(result.IsFailure);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task Given_ExistingEntity_When_GetAsync_Then_ReturnsMappedDto()
    {
        var entity = new AttachmentBuilder().Build();
        _repoQueryMock.Setup(r => r.GetAttachmentAsync(entity.Id, It.IsAny<CancellationToken>())).ReturnsAsync(entity);

        var result = await CreateService().GetAsync(entity.Id);

        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual(entity.FileName, result.Value!.Item!.FileName);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task Given_NonExistentId_When_GetAsync_Then_ReturnsNone()
    {
        _repoQueryMock.Setup(r => r.GetAttachmentAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((Attachment?)null);

        var result = await CreateService().GetAsync(Guid.NewGuid());

        Assert.IsTrue(result.IsNone);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task Given_ExistingEntity_When_UpdateAsync_Then_ReturnsSuccess()
    {
        var entity = new AttachmentBuilder().Build();
        _repoTrxnMock.Setup(r => r.GetAttachmentAsync(entity.Id, It.IsAny<CancellationToken>())).ReturnsAsync(entity);
        _repoTrxnMock.Setup(r => r.SaveChangesAsync(It.IsAny<OptimisticConcurrencyWinner>(), It.IsAny<CancellationToken>())).ReturnsAsync(0);

        var dto = new AttachmentDto
        {
            Id = entity.Id,
            FileName = "updated.pdf",
            ContentType = "application/pdf",
            FileSizeBytes = 2048,
            StorageUri = "https://storage.example.com/updated.pdf",
            OwnerType = entity.OwnerType,
            OwnerId = entity.OwnerId
        };
        var result = await CreateService().UpdateAsync(new DefaultRequest<AttachmentDto> { Item = dto });

        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual("updated.pdf", result.Value!.Item!.FileName);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task Given_NonExistentId_When_UpdateAsync_Then_ReturnsNullItem()
    {
        _repoTrxnMock.Setup(r => r.GetAttachmentAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((Attachment?)null);

        var dto = new AttachmentDto
        {
            Id = Guid.NewGuid(),
            FileName = "x.pdf",
            ContentType = "application/pdf",
            FileSizeBytes = 100,
            StorageUri = "https://storage.example.com/x.pdf",
            OwnerId = Guid.NewGuid()
        };
        var result = await CreateService().UpdateAsync(new DefaultRequest<AttachmentDto> { Item = dto });

        Assert.IsTrue(result.IsSuccess);
        Assert.IsNull(result.Value?.Item);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task Given_ExistingEntity_When_DeleteAsync_Then_ReturnsSuccess()
    {
        var entity = new AttachmentBuilder().Build();
        _repoTrxnMock.Setup(r => r.GetAttachmentAsync(entity.Id, It.IsAny<CancellationToken>())).ReturnsAsync(entity);
        _repoTrxnMock.Setup(r => r.SaveChangesAsync(It.IsAny<OptimisticConcurrencyWinner>(), It.IsAny<CancellationToken>())).ReturnsAsync(0);

        var result = await CreateService().DeleteAsync(entity.Id);

        Assert.IsTrue(result.IsSuccess);
        _repoTrxnMock.Verify(r => r.Delete(entity), Times.Once);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task Given_NonExistentId_When_DeleteAsync_Then_ReturnsSuccessIdempotent()
    {
        _repoTrxnMock.Setup(r => r.GetAttachmentAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((Attachment?)null);

        var result = await CreateService().DeleteAsync(Guid.NewGuid());

        Assert.IsTrue(result.IsSuccess);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task Given_SearchRequest_When_SearchAsync_Then_ReturnsPagedResponse()
    {
        var dtos = new List<AttachmentDto> { new() { FileName = "Test" } };
        var pagedResponse = new PagedResponse<AttachmentDto> { Data = dtos, Total = 1, PageSize = 10, PageIndex = 0 };
        _repoQueryMock.Setup(r => r.SearchAttachmentsAsync(It.IsAny<SearchRequest<AttachmentSearchFilter>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedResponse);

        var request = new SearchRequest<AttachmentSearchFilter> { PageSize = 10, PageIndex = 0 };
        var response = await CreateService().SearchAsync(request);

        Assert.AreEqual(1, response.Total);
    }
}
