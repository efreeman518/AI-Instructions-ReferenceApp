namespace Test.Mobile.PageObjects;

/// <summary>The task list: search/filter and open a task by title.</summary>
internal sealed class TaskListScreen
{
    // Accessibility labels (content-desc) from src/UI/TaskFlow.Uno/Views/TaskListPage.xaml.
    private const string SearchBoxId = "Task search input";
    private const string SearchButtonId = "Search tasks";

    private readonly MobileTaskAppDriver _app;

    public TaskListScreen(MobileTaskAppDriver app) => _app = app;

    public void WaitUntilReady() => _app.Element(SearchBoxId);

    /// <summary>Enters a search term and runs the search.</summary>
    public void Search(string term)
    {
        WaitUntilReady();
        _app.Type(SearchBoxId, term);
        _app.HideKeyboard();
        _app.Tap(SearchButtonId);
    }

    /// <summary>Opens the task whose row carries the given title (row AutomationId is bound to Title).</summary>
    public void OpenTask(string title) => _app.TapText(title);

    public void WaitForTask(string title) => _app.WaitForText(title);

    public void WaitForTaskGone(string title) => _app.WaitForTextGone(title);

    public bool HasTask(string title) => _app.HasText(title);
}
