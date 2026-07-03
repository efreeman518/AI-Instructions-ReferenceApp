using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.DependencyInjection;
using TaskFlow.Uno.Presentation.Presentation;

namespace TaskFlow.Uno.Views;

/// <summary>Hosts the task item XAML view and initializes its Uno page or control.</summary>
public sealed partial class TaskItemPage : Page
{
    /// <summary>Initializes task item page with required dependencies and default state.</summary>
    public TaskItemPage()
    {
        this.InitializeComponent();

        // Reset form state every time this page becomes visible. The
        // Visibility navigator reuses TaskItemPageModel, so state fields
        // retain their last values unless explicitly reset. Reset()
        // re-initializes all form state from Entity (empty for create,
        // entity values for edit).
        Loaded += (_, _) => RequestReset();
        RegisterPropertyChangedCallback(VisibilityProperty, (_, _) =>
        {
            if (Visibility == Visibility.Visible) RequestReset();
        });
    }

    /// <summary>Provides the request reset operation for task item page.</summary>
    private static void RequestReset() =>
        App.Host?.Services.GetService<IMessenger>()?.Send(new TaskFormResetMessage());
}
