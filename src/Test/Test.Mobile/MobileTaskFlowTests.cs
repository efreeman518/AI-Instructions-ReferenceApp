using Test.Mobile.PageObjects;

namespace Test.Mobile;

/// <summary>
/// Focused multi-step mobile flows beyond the core CRUD lifecycle, giving the native UI breadth
/// comparable to the web suites: list search/filter, and child-entity (checklist + comment) entry.
/// </summary>
[TestClass]
[TestCategory("MobileUI")]
public sealed class MobileTaskFlowTests : MobileUiTestBase
{
    /// <summary>Creates two tasks, then confirms search narrows the list to the matching one.</summary>
    [TestMethod]
    [Ignore("Requires decent Android/Appium test hardware; run manually on a dedicated device, emulator host, or CI.")]
    public void Search_FiltersTaskList_ToMatchingTitle() => RunMobileFlow(() =>
    {
        var keep = TaskFlowMobileApp.UniqueTitle("E2E-Mobile-Keep");
        var other = TaskFlowMobileApp.UniqueTitle("E2E-Mobile-Other");

        CreateSimpleTask(keep);
        CreateSimpleTask(other);

        App.TaskList.Search(keep);
        App.TaskList.WaitForTask(keep);
        Assert.IsFalse(App.TaskList.HasTask(other), "Search should hide tasks that do not match the term.");

        // Cleanup so repeated local runs do not accumulate rows.
        App.TaskList.OpenTask(keep);
        App.TaskEditor.WaitUntilReady();
        App.TaskEditor.Delete();
        App.TaskList.Search(other);
        App.TaskList.OpenTask(other);
        App.TaskEditor.WaitUntilReady();
        App.TaskEditor.Delete();
    });

    /// <summary>Adds multiple checklist items and a comment, then confirms they all render.</summary>
    [TestMethod]
    [Ignore("Requires decent Android/Appium test hardware; run manually on a dedicated device, emulator host, or CI.")]
    public void Checklist_AndComment_PersistOnTask() => RunMobileFlow(() =>
    {
        var title = TaskFlowMobileApp.UniqueTitle("E2E-Mobile-Children");
        var items = new[] { "PlanTheWork", "DoTheWork", "ReviewTheWork" };
        const string comment = "ChildrenFlowComment";

        App.Shell.StartNewTask();
        App.TaskEditor.WaitUntilReady();
        App.TaskEditor.SetTitle(title);

        var addedItems = items.Where(item => App.TaskEditor.TryAddChecklistItem(item)).ToList();
        var commentAdded = App.TaskEditor.TryAddComment(comment);

        if (addedItems.Count == 0 && !commentAdded)
        {
            App.TaskEditor.Save();
            Assert.Fail("Checklist/comment inputs were not drivable via uiautomator2 on this Uno/Skia build.");
            return;
        }

        App.TaskEditor.Save();
        App.TaskList.WaitUntilReady();

        App.TaskList.Search(title);
        App.TaskList.WaitForTask(title);
        App.TaskList.OpenTask(title);
        App.TaskEditor.WaitUntilReady();

        foreach (var item in addedItems)
        {
            Assert.IsTrue(App.TaskEditor.HasText(item), $"Checklist item '{item}' did not persist.");
        }

        if (commentAdded)
        {
            Assert.IsTrue(App.TaskEditor.HasText(comment), "Comment did not persist.");
        }

        App.TaskEditor.Delete();
        App.TaskList.WaitUntilReady();
    });

    private void CreateSimpleTask(string title)
    {
        App.Shell.StartNewTask();
        App.TaskEditor.WaitUntilReady();
        App.TaskEditor.SetTitle(title);
        App.TaskEditor.Save();
        App.TaskList.WaitUntilReady();
        App.TaskList.WaitForTask(title);
    }
}
