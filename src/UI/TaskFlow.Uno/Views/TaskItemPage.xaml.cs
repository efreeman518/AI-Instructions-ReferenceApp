using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using TaskFlow.Uno.Presentation;

namespace TaskFlow.Uno.Views;

public sealed partial class TaskItemPage : Page
{
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

    private static void RequestReset() =>
        App.Host?.Services.GetService<IMessenger>()?.Send(new TaskFormResetMessage());
}
