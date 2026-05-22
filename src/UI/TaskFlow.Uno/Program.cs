#if __WASM__
using Uno.UI.Hosting;

namespace TaskFlow.Uno;

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
