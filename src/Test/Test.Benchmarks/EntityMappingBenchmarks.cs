using BenchmarkDotNet.Attributes;
using TaskFlow.Application.Mappers;
using TaskFlow.Domain.Model;
using Test.Support.Builders;

namespace Test.Benchmarks;

/// <summary>
/// BenchmarkDotNet micro-benchmarks for entity ↔ DTO mapper hot paths (Category, TaskItem, Tag, Comment
/// ToDto plus full round-trips). Memory diagnoser enabled, 3 warm-ups + 10 iterations.
/// Benchmark tier (BenchmarkDotNet only) — runs from <c>Program.Main</c> via <c>BenchmarkSwitcher</c>;
/// not part of the MSTest run. The other tiers cannot produce reliable allocation/duration figures.
/// </summary>
[MemoryDiagnoser]
[SimpleJob(warmupCount: 3, iterationCount: 10)]
public class EntityMappingBenchmarks
{
    private Category _category = null!;
    private TaskItem _taskItem = null!;
    private Tag _tag = null!;
    private Comment _comment = null!;

    [GlobalSetup]
    public void Setup()
    {
        _category = new CategoryBuilder()
            .WithTenantId(Guid.NewGuid())
            .WithName("Benchmark Category")
            .Build();

        _tag = new TagBuilder()
            .WithTenantId(Guid.NewGuid())
            .WithName("Benchmark Tag")
            .Build();

        _taskItem = new TaskItemBuilder()
            .WithTenantId(Guid.NewGuid())
            .WithTitle("Benchmark Task")
            .Build();

        _comment = new CommentBuilder()
            .WithTenantId(Guid.NewGuid())
            .WithBody("Benchmark comment content")
            .WithTaskItemId(_taskItem.Id)
            .Build();
    }

    [Benchmark]
    public object CategoryToDto() => _category.ToDto();

    [Benchmark]
    public object TaskItemToDto() => _taskItem.ToDto();

    [Benchmark]
    public object TagToDto() => _tag.ToDto();

    [Benchmark]
    public object CommentToDto() => _comment.ToDto();

    [Benchmark]
    public object CategoryDtoRoundTrip()
    {
        var dto = _category.ToDto();
        return dto.ToEntity(_category.TenantId);
    }

    [Benchmark]
    public object TaskItemDtoRoundTrip()
    {
        var dto = _taskItem.ToDto();
        return dto.ToEntity(_taskItem.TenantId);
    }
}
