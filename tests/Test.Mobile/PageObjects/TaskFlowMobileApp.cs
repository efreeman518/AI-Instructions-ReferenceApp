using OpenQA.Selenium.Appium;

namespace Test.Mobile.PageObjects;

/// <summary>
/// Entry point for mobile UI flows. Wraps the Appium driver and exposes the TaskFlow screens
/// (shell navigation, task list, task editor) so tests read as ordered user steps. Mirrors the
/// page-object utilities the web Playwright suites use under Test.PlaywrightUI/utils.
/// </summary>
internal sealed class TaskFlowMobileApp
{
    private readonly MobileTaskAppDriver _app;

    public TaskFlowMobileApp(AppiumDriver driver, TimeSpan timeout)
    {
        _app = new MobileTaskAppDriver(driver, timeout);
        Shell = new ShellScreen(_app);
        TaskList = new TaskListScreen(_app);
        TaskEditor = new TaskItemScreen(_app);
    }

    public ShellScreen Shell { get; }
    public TaskListScreen TaskList { get; }
    public TaskItemScreen TaskEditor { get; }

    /// <summary>Collapses the notification shade if a stray gesture opened it (belt-and-suspenders).</summary>
    public void CollapseStatusBar() => _app.CollapseStatusBar();

    /// <summary>Generates a per-run unique title, mirroring the web suites' uniqueTitle helper.</summary>
    public static string UniqueTitle(string prefix) =>
        $"{SanitizeForAndroidInput(prefix)}{DateTimeOffset.UtcNow:HHmmssfff}";

    private static string SanitizeForAndroidInput(string value)
    {
        var safe = new string(value.Where(char.IsLetterOrDigit).ToArray());
        return string.IsNullOrWhiteSpace(safe) ? "Task" : safe;
    }
}
