using EF.Data.Migrations;
using Microsoft.Extensions.Logging.Abstractions;

namespace Test.Unit.Infrastructure;

[TestClass]
public sealed class DatabaseMigrationRunnerTests
{
    public TestContext TestContext { get; set; } = null!;

    [TestMethod]
    public async Task RunAsync_OrdersTargetsByOrderThenName()
    {
        var calls = new List<string>();
        var runner = new DatabaseMigrationRunner(
            [
                new RecordingTarget("third", 20, calls),
                new RecordingTarget("second", 10, calls),
                new RecordingTarget("first", 10, calls)
            ],
            NullLogger<DatabaseMigrationRunner>.Instance);

        await runner.RunAsync(TestContext.CancellationTokenSource.Token);

        CollectionAssert.AreEqual(new[] { "first", "second", "third" }, calls);
    }

    [TestMethod]
    public async Task RunAsync_StopsAtFirstFailure()
    {
        var calls = new List<string>();
        var runner = new DatabaseMigrationRunner(
            [
                new RecordingTarget("first", 10, calls),
                new FailingTarget("fail", 20, calls),
                new RecordingTarget("never", 30, calls)
            ],
            NullLogger<DatabaseMigrationRunner>.Instance);

        await Assert.ThrowsExactlyAsync<InvalidOperationException>(
            () => runner.RunAsync(TestContext.CancellationTokenSource.Token));
        CollectionAssert.AreEqual(new[] { "first", "fail" }, calls);
    }

    private sealed class RecordingTarget(string name, int order, List<string> calls) : IDatabaseMigrationTarget
    {
        public string Name { get; } = name;

        public int Order { get; } = order;

        public Task MigrateAsync(CancellationToken cancellationToken = default)
        {
            calls.Add(Name);
            return Task.CompletedTask;
        }
    }

    private sealed class FailingTarget(string name, int order, List<string> calls) : IDatabaseMigrationTarget
    {
        public string Name { get; } = name;

        public int Order { get; } = order;

        public Task MigrateAsync(CancellationToken cancellationToken = default)
        {
            calls.Add(Name);
            throw new InvalidOperationException("migration failed");
        }
    }
}
