using EF.BackgroundServices.InternalMessageBus;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TaskFlow.Application.MessageHandlers;

namespace TaskFlow.Bootstrapper;

/// <summary>
/// Host lifecycle helpers shared by API, Functions, Scheduler, and tests.
/// </summary>
public static class IHostExtensions
{
    /// <summary>
    /// Finds message handlers in the application handler assembly and registers the resolved
    /// scoped instances with the singleton internal message bus. This keeps audit handlers
    /// active without hard-coding every closed generic handler registration.
    /// </summary>
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

    /// <summary>
    /// Registers message handlers before running startup tasks so migration, warmup, and
    /// later request processing share the same internal event pipeline.
    /// </summary>
    public static async Task RunStartupTasks(this IHost host)
    {
        host.AutoRegisterMessageHandlers();

        using var scope = host.Services.CreateScope();
        foreach (var task in scope.ServiceProvider.GetServices<IStartupTask>())
            await task.ExecuteAsync();
    }
}
