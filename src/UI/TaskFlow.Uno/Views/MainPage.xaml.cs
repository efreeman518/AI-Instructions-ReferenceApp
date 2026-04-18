using Microsoft.UI.Xaml;
using Uno.Extensions.Navigation;

namespace TaskFlow.Uno.Views;

public sealed partial class MainPage : Page
{
    public MainPage()
    {
        this.InitializeComponent();
    }

    // Top-level menu: always route through an absolute path so the click
    // lands on the requested page even when the Visibility navigator is
    // currently showing a "detail" sibling (e.g. TaskItem opened from the
    // TaskList or Dashboard). Relative resolution would otherwise resolve
    // against whichever inner sibling is active and could no-op.
    private async void NavigateTopClick(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement element ||
            element.Tag is not string tag ||
            string.IsNullOrWhiteSpace(tag))
        {
            return;
        }

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

        System.Diagnostics.Debug.WriteLine($"[MainPage] NavigateTopClick → /Main/{sibling}");
        Console.WriteLine($"[MainPage] NavigateTopClick → /Main/{sibling}");

        var navigator = this.Navigator() ?? element.Navigator() ?? RootGrid?.Navigator();
        if (navigator is null) return;

        await navigator.NavigateRouteAsync(element, $"/Main/{sibling}");
    }
}
