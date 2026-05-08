using System.ComponentModel;
using TaskFlow.Uno.Core.Business.Notifications;

namespace Test.Unit.Uno;

/// <summary>
/// Validates <c>BusyTracker</c> ref-counting semantics: nested begins, double-dispose idempotence,
/// PropertyChanged firing only on the 0↔non-zero edge, and concurrent increment/decrement under
/// <c>Parallel.ForEach</c>.
/// Pure-unit tier: pure in-memory primitive; no infra.
/// </summary>
[TestClass]
[TestCategory("Unit")]
[TestCategory("Uno")]
public class BusyTrackerTests
{
    [TestMethod]
    public void Begin_Increments_And_Sets_IsActive()
    {
        var tracker = new BusyTracker();
        Assert.AreEqual(0, tracker.Pending);
        Assert.IsFalse(tracker.IsActive);

        var scope = tracker.Begin();

        Assert.AreEqual(1, tracker.Pending);
        Assert.IsTrue(tracker.IsActive);

        scope.Dispose();

        Assert.AreEqual(0, tracker.Pending);
        Assert.IsFalse(tracker.IsActive);
    }

    [TestMethod]
    public void NestedBegins_Count_Correctly()
    {
        var tracker = new BusyTracker();
        var a = tracker.Begin();
        var b = tracker.Begin();
        var c = tracker.Begin();

        Assert.AreEqual(3, tracker.Pending);

        b.Dispose();
        Assert.AreEqual(2, tracker.Pending);
        Assert.IsTrue(tracker.IsActive);

        a.Dispose();
        c.Dispose();
        Assert.AreEqual(0, tracker.Pending);
        Assert.IsFalse(tracker.IsActive);
    }

    [TestMethod]
    public void DoubleDispose_Only_Decrements_Once()
    {
        var tracker = new BusyTracker();
        var scope = tracker.Begin();

        scope.Dispose();
        scope.Dispose();

        Assert.AreEqual(0, tracker.Pending);
    }

    [TestMethod]
    public void IsActive_PropertyChanged_Fires_On_Edge_Only()
    {
        var tracker = new BusyTracker();
        var isActiveChanges = 0;
        ((INotifyPropertyChanged)tracker).PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(IBusyTracker.IsActive)) isActiveChanges++;
        };

        var a = tracker.Begin();   // 0 -> 1, edge
        var b = tracker.Begin();   // 1 -> 2, no edge
        var c = tracker.Begin();   // 2 -> 3, no edge
        a.Dispose();               // 3 -> 2
        b.Dispose();               // 2 -> 1
        c.Dispose();               // 1 -> 0, edge

        Assert.AreEqual(2, isActiveChanges);
    }

    [TestMethod]
    public async Task Concurrent_Increments_End_At_Zero()
    {
        var tracker = new BusyTracker();
        const int n = 128;

        var scopes = await Task.WhenAll(Enumerable.Range(0, n)
            .Select(_ => Task.Run(() => tracker.Begin())));

        Assert.AreEqual(n, tracker.Pending);

        Parallel.ForEach(scopes, s => s.Dispose());

        Assert.AreEqual(0, tracker.Pending);
        Assert.IsFalse(tracker.IsActive);
    }
}
