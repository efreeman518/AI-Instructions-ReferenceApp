using Uno.UI.Extensions;

namespace TaskFlow.Uno.Views;

/// <summary>Hosts the shell XAML view and initializes its Uno page or control.</summary>
public sealed partial class Shell : UserControl, IContentControlProvider
{
    public ExtendedSplashScreen SplashScreen => Splash;

    public ContentControl ContentControl => Splash;

    public Frame? RootFrame => Splash.FindFirstDescendant<Frame>();

    /// <summary>Initializes shell with required dependencies and default state.</summary>
    public Shell()
    {
        this.InitializeComponent();
    }
}
