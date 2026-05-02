using Azure.Data.Tables;
using Azure.Messaging.ServiceBus;
using Azure.Storage.Blobs;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using System;
using TaskFlow.Application.Contracts.Messaging;
using TaskFlow.Application.Contracts.Storage;
using TaskFlow.Infrastructure.Storage;
using TaskFlow.Infrastructure.Storage.CosmosDb;

namespace TaskFlow.Bootstrapper;

public static partial class RegisterServices
{
    private static void AddTableStorageServices(IServiceCollection services, IConfiguration config)
    {
        var connStr = ResolveConnectionString(
            config,
            "TableStorage1",
            "Values:TableStorage1",
            "Aspire:Azure:Data:Tables:TableStorage1:ConnectionString");
        if (string.IsNullOrEmpty(connStr))
        {
            services.AddSingleton<IAuditLogRepository, NoOpAuditLogRepository>();
            return;
        }

        services.AddAzureClients(builder =>
        {
            builder.AddTableServiceClient(connStr)
                .WithName("TaskFlowTableClient");
        });

        services.Configure<AuditLogStorageSettings>(
            config.GetSection(AuditLogStorageSettings.ConfigSectionName));

        services.AddScoped<IAuditLogRepository, AuditLogRepository>();
    }

    private static string? ResolveConnectionString(IConfiguration config, string connectionName, params string[] alternateKeys)
    {
        string? fallbackConnectionString = null;

        foreach (var candidate in GetConnectionStringCandidates(config, connectionName, alternateKeys))
        {
            if (string.IsNullOrWhiteSpace(candidate))
                continue;

            fallbackConnectionString ??= candidate;

            if (!string.Equals(candidate, "UseDevelopmentStorage=true", StringComparison.OrdinalIgnoreCase))
                return candidate;
        }

        return fallbackConnectionString;
    }

    private static IEnumerable<string?> GetConnectionStringCandidates(IConfiguration config, string connectionName, IEnumerable<string> alternateKeys)
    {
        yield return Environment.GetEnvironmentVariable($"ConnectionStrings__{connectionName}");
        yield return config.GetConnectionString(connectionName);

        foreach (var key in alternateKeys)
        {
            yield return Environment.GetEnvironmentVariable(key.Replace(":", "__"));
            yield return config[key];
        }
    }

    private static void AddBlobStorageServices(IServiceCollection services, IConfiguration config)
    {
        var connStr = ResolveConnectionString(
            config,
            "BlobStorage1",
            "BlobStorage1",
            "Values:BlobStorage1");
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
        var connStr = ResolveConnectionString(
            config,
            "ServiceBus1",
            "ServiceBus1",
            "Values:ServiceBus1");
        if (string.IsNullOrEmpty(connStr))
        {
            services.AddSingleton<IIntegrationEventPublisher, NoOpIntegrationEventPublisher>();
            return;
        }

        services.AddAzureClients(builder =>
        {
            builder.AddServiceBusClient(connStr)
                .WithName("TaskFlowSBClient");
        });

        services.AddSingleton<IIntegrationEventPublisher, ServiceBusIntegrationEventPublisher>();
    }

    private static void AddCosmosDbServices(IServiceCollection services, IConfiguration config)
    {
        var connStr = config.GetConnectionString("CosmosDb1");
        if (string.IsNullOrEmpty(connStr))
        {
            services.AddSingleton<ITaskViewRepository, NoOpTaskViewRepository>();
            return;
        }

        var databaseName = config["Cosmos:TaskViews:DatabaseName"] ?? "taskflow-db";
        var containerName = config["Cosmos:TaskViews:ContainerName"] ?? "task-views";

        services.AddSingleton(_ => new Microsoft.Azure.Cosmos.CosmosClient(connStr));
        services.AddSingleton<ITaskViewRepository>(sp =>
            new CosmosTaskViewRepository(
                sp.GetRequiredService<Microsoft.Azure.Cosmos.CosmosClient>(),
                sp.GetRequiredService<ILogger<CosmosTaskViewRepository>>(),
                databaseName,
                containerName));
    }

    private static void AddHealthChecks(IServiceCollection services)
    {
        services.AddHealthChecks()
            .AddCheck<HealthChecks.SqlHealthCheck>("sql", tags: ["ready"]);
    }
}
