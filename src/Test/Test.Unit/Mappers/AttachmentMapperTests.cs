using TaskFlow.Application.Mappers;
using TaskFlow.Application.Models;
using TaskFlow.Domain.Model;
using TaskFlow.Domain.Shared.Enums;
using Test.Support;
using Test.Support.Builders;

namespace Test.Unit.Mappers;

[TestClass]
public class AttachmentMapperTests
{
    [TestMethod]
    [TestCategory("Unit")]
    public void Given_ValidEntity_When_MappedToDto_Then_AllPropertiesMapped()
    {
        var ownerId = Guid.NewGuid();
        var entity = new AttachmentBuilder().WithOwnerId(ownerId).WithOwnerType(AttachmentOwnerType.TaskItem).Build();
        var dto = entity.ToDto();

        Assert.AreEqual(entity.Id, dto.Id);
        Assert.AreEqual(entity.FileName, dto.FileName);
        Assert.AreEqual(entity.ContentType, dto.ContentType);
        Assert.AreEqual(entity.FileSizeBytes, dto.FileSizeBytes);
        Assert.AreEqual(entity.StorageUri, dto.StorageUri);
        Assert.AreEqual(entity.OwnerType, dto.OwnerType);
        Assert.AreEqual(entity.OwnerId, dto.OwnerId);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public void Given_ValidDto_When_MappedToEntity_Then_ReturnsSuccessDomainResult()
    {
        var ownerId = Guid.NewGuid();
        var dto = new AttachmentDto
        {
            FileName = "report.pdf",
            ContentType = "application/pdf",
            FileSizeBytes = 2048,
            StorageUri = "https://storage.example.com/report.pdf",
            OwnerType = AttachmentOwnerType.TaskItem,
            OwnerId = ownerId
        };
        var result = dto.ToEntity(TestConstants.TenantId);

        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual("report.pdf", result.Value!.FileName);
        Assert.AreEqual(ownerId, result.Value.OwnerId);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public void Given_InvalidDto_When_MappedToEntity_Then_ReturnsFailure()
    {
        var dto = new AttachmentDto { FileName = "", ContentType = "", FileSizeBytes = 0, StorageUri = "", OwnerId = Guid.Empty };
        var result = dto.ToEntity(TestConstants.TenantId);

        Assert.IsTrue(result.IsFailure);
    }
}
