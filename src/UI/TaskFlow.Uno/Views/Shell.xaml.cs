using Microsoft.UI.Xaml.Controls;
using Uno.Toolkit.UI;
using Uno.UI.Extensions;

namespace TaskFlow.Uno.Views;

public sealed partial class Shell : UserControl, IContentControlProvider
{
    public ExtendedSplashScreen SplashScreen => Splash;

    public ContentControl ContentControl => Splash;

    public Frame? RootFrame => Splash.FindFirstDescendant<Frame>();

    public Shell()
    {
        this.InitializeComponent();
    }
}
