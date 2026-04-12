using Microsoft.Extensions.Hosting;
using TaskFlow.Uno.Views;

namespace TaskFlow.Uno;

public partial class App : Application
{
    public static Window? MainWindow;
    public static IHost? Host { get; private set; }

    public App()
    {
        this.InitializeComponent();
    }

    protected override async void OnLaunched(LaunchActivatedEventArgs args)
    {
        var builder = this.CreateBuilder(args);
        ConfigureAppBuilder(builder);
        MainWindow = builder.Window;

        Host = await builder.NavigateAsync<Shell>();
    }
}
