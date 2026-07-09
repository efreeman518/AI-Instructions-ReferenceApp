using Test.Mobile.PageObjects;

namespace Test.Mobile;

/// <summary>
/// Native mobile form-entry smoke. Persistence CRUD is covered by API, unit,
/// integration, and Playwright lanes; Appium stays focused on native shell health.
/// </summary>
[TestClass]
[TestCategory("MobileUI")]
public sealed class MobileTaskCrudTests : MobileUiTestBase
{
    [TestMethod]
    [Timeout(180_000, CooperativeCancellation = true)]
    public void TaskEditor_FirstViewportFields_AcceptNativeText() => RunMobileFlow(() =>
    {
        var title = TaskFlowMobileApp.UniqueTitle("MobileText");
        App.Shell.StartNewTask();
        App.TaskEditor.WaitUntilReady();
        App.TaskEditor.SetTitle(title);

        Assert.IsTrue(App.TaskEditor.HasText(title), "Title was not visible after native text entry.");
    });
}
