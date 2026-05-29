using Uno.UI.Hosting;

namespace TaskFlow.Uno.iOS;

/// <summary>Provides platform entry behavior for the Uno entry point target.</summary>
public class EntryPoint
{
    /// <summary>Provides the main operation for entry point.</summary>
    public static void Main(string[] args)
    {
        var host = UnoPlatformHostBuilder.Create()
            .App(() => new App())
            .UseAppleUIKit()
            .Build();

        host.Run();
    }
}
