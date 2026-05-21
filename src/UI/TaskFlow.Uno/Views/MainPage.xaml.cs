using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using TaskFlow.Uno.Presentation;
using Uno.Extensions.Navigation;

namespace TaskFlow.Uno.Views;

public sealed partial class MainPage : Page
{
    public MainPage()
    {
        this.InitializeComponent();
    }

    // Top-level menu: a click on Dashboard / Tasks / Categories / Tags /
    // Settings / New Task MUST always land on the requested sibling even
    // when a "detail" sibling (TaskItem) is currently visible. If the
    // detail form has unsaved edits we prompt first so the user doesn't
    // silently lose them.
    private async void NavigateTopClick(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement element ||
            element.Tag is not string tag ||
            string.IsNullOrWhiteSpace(tag))
        {
            Console.WriteLine("[MainPage] NavigateTopClick: invalid sender/tag");
            return;
        }

        Console.WriteLine($"[MainPage] NavigateTopClick tag={tag}");

        var sibling = tag switch
        {
            "Dashboard" => "Dashboard",
            "TaskList" => "TaskList",
            "Categories" => "Categories",
            "Tags" => "Tags",
            "TaskItem" => "TaskItem",
            "Settings" => "Settings",
            _ => null,
        };
        if (sibling is null) return;

        var services = App.Host?.Services;
        var guard = services?.GetService<IFormGuard>();
        if (guard?.IsDirtyAsync is { } isDirty)
        {
            bool dirty;
            try { dirty = await isDirty(default); }
            catch (Exception ex)
            {
                Console.WriteLine($"[MainPage] IsDirtyAsync threw: {ex.Message}");
                dirty = false;
            }

            Console.WriteLine($"[MainPage] dirty={dirty}");

            if (dirty && !await ConfirmDiscardAsync())
            {
                return;
            }
            guard.Clear();
        }

        await NavigateToSiblingAsync(element, sibling);
    }

    // Try progressively more forceful strategies. The original "/Main/X"
    // rooted path silently no-ops because it dispatches to the Shell's
    // FrameNavigator (which already has MainPage loaded and reports
    // success without descending into the Visibility sub-region). The
    // relative sibling route on the composite / inner navigator is what
    // actually swaps Visibility under Main.
    private async Task NavigateToSiblingAsync(FrameworkElement element, string sibling)
    {
        var composite = this.Navigator();
        var inner = RootGrid?.Navigator();

        Console.WriteLine($"[MainPage] composite={composite?.GetType().Name ?? "null"} inner={inner?.GetType().Name ?? "null"}");

        // Strategy 1: inner visibility-region navigator, relative route.
        // This is the closest match to what the Visibility navigator
        // expects and directly manages sibling visibility.
        if (inner is not null)
        {
            var resp = await inner.NavigateRouteAsync(this, sibling);
            Console.WriteLine($"[MainPage] inner {sibling} → success={resp?.Success}");
            if (resp?.Success == true)
            {
                ForceSiblingVisibility(sibling);
                return;
            }
        }

        // Strategy 2: composite navigator on the parent page, relative
        // sibling route. Composite dispatches down into the visibility
        // region.
        if (composite is not null && !ReferenceEquals(composite, inner))
        {
            var resp = await composite.NavigateRouteAsync(this, sibling);
            Console.WriteLine($"[MainPage] composite {sibling} → success={resp?.Success}");
            if (resp?.Success == true)
            {
                ForceSiblingVisibility(sibling);
                return;
            }
        }

        // Strategy 3: navigate by ViewModel type — bypasses route-string
        // resolution and targets the registered ViewMap directly.
        var typed = inner ?? composite ?? element.Navigator();
        if (typed is not null)
        {
            var resp = sibling switch
            {
                "Dashboard" => await typed.NavigateViewModelAsync<DashboardModel>(this),
                "TaskList" => await typed.NavigateViewModelAsync<TaskListModel>(this),
                "Categories" => await typed.NavigateViewModelAsync<CategoryTreeModel>(this),
                "Tags" => await typed.NavigateViewModelAsync<TagManagementModel>(this),
                "TaskItem" => await typed.NavigateViewModelAsync<TaskItemPageModel>(this),
                "Settings" => await typed.NavigateViewModelAsync<SettingsModel>(this),
                _ => null,
            };
            Console.WriteLine($"[MainPage] viewmodel {sibling} → success={resp?.Success}");
        }
    }

    // PanelVisibilityNavigator is observed to flip the target sibling to
    // Visible but leave the previously-active detail (TaskItem) also
    // Visible — so the two overlap and the detail paints on top. Inspect
    // RootGrid children via several identifiers (Region.Name attached
    // prop, element Name, hosted-content type) and ONLY enforce
    // visibility if we positively identify the target child. Forcing
    // when nothing matches would blank the whole content region.
    private void ForceSiblingVisibility(string sibling)
    {
        if (RootGrid is null) return;

        var targetTypeName = sibling + "Page";
        var matched = new List<FrameworkElement>();
        var all = new List<FrameworkElement>();

        foreach (var child in RootGrid.Children)
        {
            if (child is not FrameworkElement fe) continue;
            all.Add(fe);

            var regionName = global::Uno.Extensions.Navigation.UI.Region.GetName(fe) ?? string.Empty;
            var elementName = fe.Name ?? string.Empty;
            var contentTypeName = (fe is ContentControl cc && cc.Content is not null)
                ? cc.Content.GetType().Name
                : string.Empty;
            var selfTypeName = fe.GetType().Name;

            Console.WriteLine($"[MainPage] child type={selfTypeName} name='{elementName}' region='{regionName}' content='{contentTypeName}'");

            var hit = string.Equals(regionName, sibling, StringComparison.Ordinal)
                   || string.Equals(elementName, sibling, StringComparison.Ordinal)
                   || string.Equals(selfTypeName, targetTypeName, StringComparison.Ordinal)
                   || string.Equals(contentTypeName, targetTypeName, StringComparison.Ordinal);

            if (hit) matched.Add(fe);
        }

        if (matched.Count == 0)
        {
            Console.WriteLine($"[MainPage] force: no child matched '{sibling}' — leaving visibility alone");
            return;
        }

        foreach (var fe in all)
        {
            var shouldShow = matched.Contains(fe);
            fe.Visibility = shouldShow ? Visibility.Visible : Visibility.Collapsed;
            Console.WriteLine($"[MainPage] force {fe.GetType().Name}.Visibility={fe.Visibility}");

            // Each FrameView hosts a Frame that may have stacked detail
            // pages (TaskItem pushed onto TaskList's Frame, for example).
            // Top-menu navigation must land the user on the sibling's
            // ROOT page, not whatever detail was left on its stack — so
            // pop every FrameView's inner Frame to its root.
            PopFrameToRoot(fe);
        }
    }

    private static void PopFrameToRoot(FrameworkElement host)
    {
        var frame = FindChildFrame(host);
        if (frame is null) return;

        var pops = 0;
        while (frame.CanGoBack && pops < 32)
        {
            frame.GoBack();
            pops++;
        }

        if (pops > 0)
        {
            Console.WriteLine($"[MainPage] popped {pops} frame(s) from {host.GetType().Name}");
        }
    }

    private static Frame? FindChildFrame(DependencyObject root)
    {
        if (root is Frame f) return f;
        var count = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetChildrenCount(root);
        for (var i = 0; i < count; i++)
        {
            var child = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetChild(root, i);
            var found = FindChildFrame(child);
            if (found is not null) return found;
        }
        if (root is ContentControl cc && cc.Content is DependencyObject content)
        {
            return FindChildFrame(content);
        }
        return null;
    }

    private async Task<bool> ConfirmDiscardAsync()
    {
        var dialog = new ContentDialog
        {
            Title = "Discard unsaved changes?",
            Content = "You have unsaved edits on this task. Leave the page and discard them?",
            PrimaryButtonText = "Discard",
            CloseButtonText = "Stay",
            DefaultButton = ContentDialogButton.Close,
            XamlRoot = this.XamlRoot,
        };
        var result = await dialog.ShowAsync();
        return result == ContentDialogResult.Primary;
    }
}
