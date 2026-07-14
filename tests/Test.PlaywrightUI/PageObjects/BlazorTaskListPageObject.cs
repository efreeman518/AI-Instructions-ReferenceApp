using Microsoft.Playwright;

namespace Test.PlaywrightUI.PageObjects;

/// <summary>
/// Wraps the stable Blazor task-list happy path selectors.
/// </summary>
public sealed class BlazorTaskListPageObject(IPage page)
{
    private const int NavigationTimeout = 60_000;

    /// <summary>
    /// Navigates to the task list and waits for the page shell and server-backed table controls.
    /// </summary>
    public async Task NavigateAndAssertReadyAsync(string baseUrl)
    {
        await page.GotoAsync(
            $"{baseUrl.TrimEnd('/')}/tasks",
            new PageGotoOptions
            {
                WaitUntil = WaitUntilState.DOMContentLoaded,
                Timeout = NavigationTimeout
            });

        await HeadingLocator().WaitForAsync(new LocatorWaitForOptions
        {
            State = WaitForSelectorState.Visible,
            Timeout = 15_000
        });

        await page.Locator(".mud-table").WaitForAsync(new LocatorWaitForOptions
        {
            State = WaitForSelectorState.Visible,
            Timeout = 15_000
        });

        await SearchInputLocator().WaitForAsync(new LocatorWaitForOptions
        {
            State = WaitForSelectorState.Visible,
            Timeout = 10_000
        });

        await NewTaskButtonLocator().WaitForAsync(new LocatorWaitForOptions
        {
            State = WaitForSelectorState.Visible,
            Timeout = 10_000
        });
    }

    /// <summary>
    /// Returns the task-list heading locator.
    /// </summary>
    public ILocator HeadingLocator() =>
        page.GetByRole(AriaRole.Heading, new PageGetByRoleOptions { Name = "Tasks" });

    /// <summary>
    /// Returns the task-list search field locator.
    /// </summary>
    public ILocator SearchInputLocator() =>
        page.GetByPlaceholder("Title or description");

    /// <summary>
    /// Returns the new-task action locator.
    /// </summary>
    public ILocator NewTaskButtonLocator() =>
        page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "New Task" });
}
