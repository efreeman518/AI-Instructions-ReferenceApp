using EF.BackgroundServices;
using EF.BackgroundServices.InternalMessageBus;
using EF.BackgroundServices.Work;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TaskFlow.Bootstrapper.StartupTasks;
using TaskFlow.Infrastructure.AI;

namespace TaskFlow.Bootstrapper;

public static partial class RegisterServices
{
    public static IServiceCollection RegisterInfrastructureServices(
        this IServiceCollection services, IConfiguration config)
    {
        services.AddSingleton(TimeProvider.System);
        services.AddSupportServices();

        AddRequestContext(services);
        AddDatabaseServices(services, config);
        AddCachingServices(services, config);
        AddTableStorageServices(services, config);
        AddBlobStorageServices(services, config);
        AddServiceBusServices(services, config);
        AddCosmosDbServices(services, config);
        AddHealthChecks(services);
        AddStartupTasks(services);

        return services;
    }

    public static IServiceCollection RegisterDomainServices(
        this IServiceCollection services, IConfiguration config)
    {
        // Domain services (if any)
        return services;
    }

    public static IServiceCollection RegisterApplicationServices(
        this IServiceCollection services, IConfiguration config)
    {
        AddApplicationServices(services);
        services.AddAiServices(config);
        return services;
    }

    public static IServiceCollection RegisterBackgroundServices(
        this IServiceCollection services, IConfiguration config)
    {
        // Channel-based background queue already registered in AddSupportServices
        return services;
    }

    private static void AddStartupTasks(IServiceCollection services)
    {
        services.AddScoped<IStartupTask, ApplyEFMigrationsStartup>();
        services.AddScoped<IStartupTask, WarmupDependencies>();
    }

    private static IServiceCollection AddSupportServices(this IServiceCollection services)
    {
        services.AddChannelBackgroundTaskQueueWithShutdownHandling();
        services.AddSingleton<IInternalMessageBus, InternalMessageBus>();
        return services;
    }
}
