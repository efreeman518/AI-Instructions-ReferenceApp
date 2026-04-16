using EF.BackgroundServices.InternalMessageBus;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace TaskFlow.Bootstrapper;

public static class IHostExtensions
{
    public static async Task RunStartupTasks(this IHost host)
    {
        var msgBus = host.Services.GetRequiredService<IInternalMessageBus>();
        msgBus.AutoRegisterHandlers();

        using var scope = host.Services.CreateScope();
        foreach (var task in scope.ServiceProvider.GetServices<IStartupTask>())
            await task.ExecuteAsync();
    }
}
