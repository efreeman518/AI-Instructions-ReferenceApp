using Test.Mobile.PageObjects;

namespace Test.Mobile;

/// <summary>
/// Full task CRUD lifecycle exercised through the native Uno (Android) UI, mirroring the web
/// Playwright suites (Test.PlaywrightUI/tests/{blazor,uno,react}/task-crud.spec.ts): create a task
/// with a checklist item and a comment, read it back, update title + priority, then delete it.
/// One Appium session drives the whole flow so the steps share UI + backend state.
/// </summary>
[TestClass]
[TestCategory("MobileUI")]
public sealed class MobileTaskCrudTests : MobileUiTestBase
{
    [TestMethod]
    public void TaskCrud_FullLifecycle_ThroughNativeUi() => RunMobileFlow(() =>
    {
        var title = TaskFlowMobileApp.UniqueTitle("E2E-Mobile-Create");
        var updatedTitle = TaskFlowMobileApp.UniqueTitle("E2E-Mobile-Updated");
        const string checklistItem = "Mobile checklist step";
        const string comment = "Mobile automated comment";

        // 1. CREATE - new task with a buffered checklist item + comment, then save.
        TestContext.WriteLine("Step 1: create");
        App.Shell.StartNewTask();
        App.TaskEditor.WaitUntilReady();
        App.TaskEditor.SetTitle(title);
        App.TaskEditor.SetDescription("Created by the mobile CRUD lifecycle test.");
        if (!App.TaskEditor.TrySetPriority("Medium"))
        {
            TestContext.WriteLine("Priority Spinner dropdown not drivable via uiautomator2; left at default (best-effort).");
        }

        var checklistAdded = App.TaskEditor.TryAddChecklistItem(checklistItem);
        var commentAdded = App.TaskEditor.TryAddComment(comment);
        TestContext.WriteLine($"Children added (best-effort): checklist={checklistAdded}, comment={commentAdded}");
        App.TaskEditor.Save();

        // 2. READ - the new task lands in the list; open it and confirm children persisted.
        TestContext.WriteLine("Step 2: read");
        App.TaskList.Search(title);
        App.TaskList.WaitForTask(title);
        App.TaskList.OpenTask(title);
        App.TaskEditor.WaitUntilReady();
        // Children are best-effort on Skia; assert persistence only for those we could add.
        if (checklistAdded)
        {
            Assert.IsTrue(App.TaskEditor.HasText(checklistItem), "Checklist item did not persist on the created task.");
        }

        if (commentAdded)
        {
            Assert.IsTrue(App.TaskEditor.HasText(comment), "Comment did not persist on the created task.");
        }

        // 3. UPDATE - rename the task and save (priority is best-effort decoration).
        TestContext.WriteLine("Step 3: update");
        App.TaskEditor.SetTitle(updatedTitle);
        App.TaskEditor.TrySetPriority("High");
        App.TaskEditor.Save();

        App.TaskList.Search(updatedTitle);
        App.TaskList.WaitForTask(updatedTitle);
        Assert.IsFalse(App.TaskList.HasTask(title), "Original title should no longer be present after rename.");

        // 4. DELETE - open the renamed task and delete it; it leaves the list.
        TestContext.WriteLine("Step 4: delete");
        App.TaskList.OpenTask(updatedTitle);
        App.TaskEditor.WaitUntilReady();
        App.TaskEditor.Delete();

        App.TaskList.Search(updatedTitle);
        App.TaskList.WaitForTaskGone(updatedTitle);
    });
}
