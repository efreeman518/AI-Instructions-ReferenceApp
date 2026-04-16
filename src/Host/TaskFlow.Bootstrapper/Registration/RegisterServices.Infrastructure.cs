using Azure.Messaging.ServiceBus;
using Azure.Storage.Blobs;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TaskFlow.Application.Contracts.Messaging;
using TaskFlow.Application.Contracts.Storage;
using TaskFlow.Infrastructure.Storage;
using TaskFlow.Infrastructure.Storage.CosmosDb;

namespace TaskFlow.Bootstrapper;

public static partial class RegisterServices
{
    private static void AddBlobStorageServices(IServiceCollection services, IConfiguration config)
    {
        var connStr = config.GetConnectionString("BlobStorage1");
        if (string.IsNullOrEmpty(connStr)) return;

        services.AddAzureClients(builder =>
        {
            builder.AddBlobServiceClient(connStr)
                .WithName("TaskFlowBlobClient");
        });

        services.Configure<BlobStorageSettings>(
            config.GetSection("BlobStorageSettings"));

        services.AddScoped<IBlobStorageRepository, BlobStorageRepository>();
    }

    private static void AddServiceBusServices(IServiceCollection services, IConfiguration config)
    {
        var connStr = config.GetConnectionString("ServiceBus1");
        if (string.IsNullOrEmpty(connStr))
        {
            services.AddSingleton<IDomainEventPublisher, NoOpDomainEventPublisher>();
            return;
        }

        services.AddAzureClients(builder =>
        {
            builder.AddServiceBusClient(connStr)
                .WithName("TaskFlowSBClient");
        });

        services.AddSingleton<IDomainEventPublisher, ServiceBusDomainEventPublisher>();
    }

    private static void AddCosmosDbServices(IServiceCollection services, IConfiguration config)
    {
        var connStr = config.GetConnectionString("CosmosDb1");
        if (string.IsNullOrEmpty(connStr))
        {
            services.AddSingleton<ITaskViewRepository, NoOpTaskViewRepository>();
            return;
        }

        services.AddSingleton(_ => new Microsoft.Azure.Cosmos.CosmosClient(connStr));
        services.AddSingleton<ITaskViewRepository, CosmosTaskViewRepository>();
    }

    private static void AddHealthChecks(IServiceCollection services)
    {
        services.AddHealthChecks()
            .AddCheck<HealthChecks.SqlHealthCheck>("sql", tags: ["ready"]);
    }
}
