#if __WASM__
using Uno.UI.Hosting;

namespace TaskFlow.Uno;

/// <summary>Bootstraps the Uno application host for the selected platform target.</summary>
public class Program
{
    static async Task Main(string[] args)
    {
        var host = UnoPlatformHostBuilder.Create()
            .App(() => new App())
            .UseWebAssembly()
            .Build();

        await host.RunAsync();
    }
}
#endif
