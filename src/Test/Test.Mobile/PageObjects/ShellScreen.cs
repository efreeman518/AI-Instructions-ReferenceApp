namespace Test.Mobile.PageObjects;

/// <summary>Shell chrome: the bottom navigation bar and the floating "new task" action.</summary>
internal sealed class ShellScreen
{
    // Accessibility labels (AutomationProperties.Name / content-desc) from MainPage.xaml.
    // Uno on Android exposes Name as content-desc; AutomationId is not surfaced there.
    private const string NewTaskButtonId = "Create new task";
    private const string NavTasksId = "Tasks";
    private const string NavDashboardId = "Home";

    private readonly MobileTaskAppDriver _app;

    public ShellScreen(MobileTaskAppDriver app) => _app = app;

    /// <summary>Taps the floating action button to open the task editor in create mode.</summary>
    public void StartNewTask() => _app.Tap(NewTaskButtonId);

    /// <summary>Navigates to the task list tab.</summary>
    public void GoToTasks() => _app.Tap(NavTasksId);

    /// <summary>Navigates to the dashboard tab.</summary>
    public void GoToDashboard() => _app.Tap(NavDashboardId);
}
