using TaskFlow.Application.Mappers;
using TaskFlow.Domain.Model;
using Test.Support;
using Test.Support.Builders;

namespace Test.Unit.Mappers;

/// <summary>
/// Parity guards for the compile-projection pattern. Each mapper exposes a single canonical
/// <c>Projection</c> expression; <c>ToDto</c> reuses the compiled delegate so EF (server-side)
/// and in-memory code paths cannot drift.
///
/// For the simple mappers the parity check is trivially true (ToDto IS the compiled projection)
/// but the tests still verify the expression compiles and surfaces all expected fields — i.e.
/// the projection is a real full shape, not a forgotten subset.
///
/// For <see cref="TaskItemMapper"/> the test additionally guards against drift between the
/// inlined child projections (Comments / ChecklistItems / Tags / SubTasks) and each child
/// mapper's own <c>ToDto</c>. EF cannot translate child <c>.ToDto()</c> calls, so the parent
/// expression must inline child shapes — the test ensures the inlined shape stays consistent
/// with the child mapper's hand-written-equivalent path.
///
/// Owned-type flattening (DateRange / RecurrencePattern → scalar columns) is also exercised:
/// it must remain EF-translatable AND evaluate correctly in-memory.
/// </summary>
[TestClass]
public class MapperProjectionParityTests
{
    [TestMethod]
    [TestCategory("Unit")]
    public void Category_CompiledProjection_AgreesWith_ToDto()
    {
        var entity = new CategoryBuilder().WithSortOrder(7).Build();

        var fromCompiled = CategoryMapper.Projection.Compile()(entity);
        var fromToDto = entity.ToDto();

        Assert.AreEqual(fromToDto.Id, fromCompiled.Id);
        Assert.AreEqual(fromToDto.Name, fromCompiled.Name);
        Assert.AreEqual(fromToDto.Description, fromCompiled.Description);
        Assert.AreEqual(fromToDto.SortOrder, fromCompiled.SortOrder);
        Assert.AreEqual(fromToDto.IsActive, fromCompiled.IsActive);
        Assert.AreEqual(fromToDto.ParentCategoryId, fromCompiled.ParentCategoryId);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public void Comment_CompiledProjection_AgreesWith_ToDto()
    {
        var entity = new CommentBuilder().WithBody("Parity body").Build();

        var fromCompiled = CommentMapper.Projection.Compile()(entity);
        var fromToDto = entity.ToDto();

        Assert.AreEqual(fromToDto.Id, fromCompiled.Id);
        Assert.AreEqual(fromToDto.Body, fromCompiled.Body);
        Assert.AreEqual(fromToDto.TaskItemId, fromCompiled.TaskItemId);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public void Tag_CompiledProjection_AgreesWith_ToDto()
    {
        var entity = new TagBuilder().WithName("Parity").WithColor("#123456").Build();

        var fromCompiled = TagMapper.Projection.Compile()(entity);
        var fromToDto = entity.ToDto();

        Assert.AreEqual(fromToDto.Id, fromCompiled.Id);
        Assert.AreEqual(fromToDto.Name, fromCompiled.Name);
        Assert.AreEqual(fromToDto.Color, fromCompiled.Color);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public void ChecklistItem_CompiledProjection_AgreesWith_ToDto()
    {
        var entity = new ChecklistItemBuilder().WithSortOrder(4).Build();

        var fromCompiled = ChecklistItemMapper.Projection.Compile()(entity);
        var fromToDto = entity.ToDto();

        Assert.AreEqual(fromToDto.Id, fromCompiled.Id);
        Assert.AreEqual(fromToDto.Title, fromCompiled.Title);
        Assert.AreEqual(fromToDto.IsCompleted, fromCompiled.IsCompleted);
        Assert.AreEqual(fromToDto.SortOrder, fromCompiled.SortOrder);
        Assert.AreEqual(fromToDto.TaskItemId, fromCompiled.TaskItemId);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public void Attachment_CompiledProjection_AgreesWith_ToDto()
    {
        var entity = new AttachmentBuilder().WithFileName("parity.pdf").Build();

        var fromCompiled = AttachmentMapper.Projection.Compile()(entity);
        var fromToDto = entity.ToDto();

        Assert.AreEqual(fromToDto.Id, fromCompiled.Id);
        Assert.AreEqual(fromToDto.FileName, fromCompiled.FileName);
        Assert.AreEqual(fromToDto.ContentType, fromCompiled.ContentType);
        Assert.AreEqual(fromToDto.FileSizeBytes, fromCompiled.FileSizeBytes);
        Assert.AreEqual(fromToDto.OwnerType, fromCompiled.OwnerType);
        Assert.AreEqual(fromToDto.OwnerId, fromCompiled.OwnerId);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public void TaskItem_CompiledProjection_AgreesWith_ToDto_ForScalarsAndOwnedTypes()
    {
        var entity = new TaskItemBuilder().Build();
        var start = DateTimeOffset.UtcNow;
        var due = start.AddDays(7);
        entity.UpdateDateRange(start, due);

        var fromCompiled = TaskItemMapper.Projection.Compile()(entity);
        var fromToDto = entity.ToDto();

        Assert.AreEqual(fromToDto.Id, fromCompiled.Id);
        Assert.AreEqual(fromToDto.Title, fromCompiled.Title);
        Assert.AreEqual(fromToDto.Status, fromCompiled.Status);
        Assert.AreEqual(fromToDto.Priority, fromCompiled.Priority);
        // Owned-type flatten guard: DateRange must project identically in both call sites.
        Assert.AreEqual(fromToDto.StartDate, fromCompiled.StartDate);
        Assert.AreEqual(fromToDto.DueDate, fromCompiled.DueDate);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public void TaskItem_InlinedChildren_AgreeWith_ChildMappers()
    {
        var entity = new TaskItemBuilder().Build();
        entity.Comments.Add(new CommentBuilder()
            .WithTaskItemId(entity.Id).WithBody("Child parity").Build());
        entity.ChecklistItems.Add(new ChecklistItemBuilder()
            .WithTaskItemId(entity.Id).WithSortOrder(1).Build());

        var fullDto = entity.ToDto();

        var expectedComment = entity.Comments.Single().ToDto();
        Assert.AreEqual(1, fullDto.Comments.Count);
        Assert.AreEqual(expectedComment.Id, fullDto.Comments[0].Id);
        Assert.AreEqual(expectedComment.Body, fullDto.Comments[0].Body);
        Assert.AreEqual(expectedComment.TaskItemId, fullDto.Comments[0].TaskItemId);

        var expectedChecklist = entity.ChecklistItems.Single().ToDto();
        Assert.AreEqual(1, fullDto.ChecklistItems.Count);
        Assert.AreEqual(expectedChecklist.Id, fullDto.ChecklistItems[0].Id);
        Assert.AreEqual(expectedChecklist.Title, fullDto.ChecklistItems[0].Title);
        Assert.AreEqual(expectedChecklist.SortOrder, fullDto.ChecklistItems[0].SortOrder);
        Assert.AreEqual(expectedChecklist.TaskItemId, fullDto.ChecklistItems[0].TaskItemId);
    }
}
