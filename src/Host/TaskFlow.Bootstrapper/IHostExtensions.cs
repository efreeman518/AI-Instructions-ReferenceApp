using EF.BackgroundServices.InternalMessageBus;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TaskFlow.Application.MessageHandlers;

namespace TaskFlow.Bootstrapper;

public static class IHostExtensions
{
    public static void AutoRegisterMessageHandlers(this IHost host)
    {
        var msgBus = host.Services.GetRequiredService<IInternalMessageBus>();

        var handlerAssembly = typeof(AuditHandler).Assembly;
        var handlerInterfaceType = typeof(IMessageHandler<>);
        var registerMethod = typeof(IInternalMessageBus)
            .GetMethods()
            .Single(method => method.Name == nameof(IInternalMessageBus.RegisterMessageHandler));

        var handlerRegistrations = handlerAssembly
            .GetTypes()
            .Where(type => !type.IsAbstract && !type.IsInterface)
            .SelectMany(type => type
                .GetInterfaces()
                .Where(@interface => @interface.IsGenericType &&
                    @interface.GetGenericTypeDefinition() == handlerInterfaceType)
                .Select(@interface => new { HandlerType = type, HandlerInterface = @interface }))
            .GroupBy(item => new { item.HandlerType, item.HandlerInterface })
            .Select(group => group.First())
            .ToList();

        using var scope = host.Services.CreateScope();

        foreach (var registration in handlerRegistrations)
        {
            var handlers = scope.ServiceProvider.GetServices(registration.HandlerInterface);
            var messageType = registration.HandlerInterface.GetGenericArguments()[0];
            var closedRegisterMethod = registerMethod.MakeGenericMethod(messageType);

            foreach (var handler in handlers)
            {
                closedRegisterMethod.Invoke(msgBus, [handler]);
            }
        }
    }

    public static async Task RunStartupTasks(this IHost host)
    {
        host.AutoRegisterMessageHandlers();

        using var scope = host.Services.CreateScope();
        foreach (var task in scope.ServiceProvider.GetServices<IStartupTask>())
            await task.ExecuteAsync();
    }
}
