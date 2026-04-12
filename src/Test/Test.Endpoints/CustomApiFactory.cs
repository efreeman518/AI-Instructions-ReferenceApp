using EF.Data;
using EF.Data.Interceptors;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using TaskFlow.Infrastructure.Data;

namespace Test.Endpoints;

public class CustomApiFactory : WebApplicationFactory<Program>
{
    private readonly string _dbName = $"TestDb_{Guid.NewGuid()}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.ConfigureServices(services =>
        {
            // Remove hosted services to avoid background work during tests
            services.RemoveAll<IHostedService>();

            // Remove interceptors (AuditInterceptor needs IInternalMessageBus)
            services.RemoveAll<AuditInterceptor<string, Guid?>>();
            services.RemoveAll<ConnectionNoLockInterceptor>();

            // Remove all EF pooling/factory/context registrations
            RemoveDescriptorsByServiceType(services, typeof(TaskFlowDbContextTrxn));
            RemoveDescriptorsByServiceType(services, typeof(TaskFlowDbContextQuery));
            RemoveDescriptorsByServiceType(services, typeof(DbContextOptions<TaskFlowDbContextTrxn>));
            RemoveDescriptorsByServiceType(services, typeof(DbContextOptions<TaskFlowDbContextQuery>));
            RemoveDescriptorsByServiceType(services, typeof(DbContextOptions));
            RemoveDescriptorsByServiceType(services, typeof(IDbContextFactory<TaskFlowDbContextTrxn>));
            RemoveDescriptorsByServiceType(services, typeof(IDbContextFactory<TaskFlowDbContextQuery>));
            RemoveDescriptorsByServiceType(services, typeof(DbContextScopedFactory<TaskFlowDbContextTrxn, string, Guid?>));
            RemoveDescriptorsByServiceType(services, typeof(DbContextScopedFactory<TaskFlowDbContextQuery, string, Guid?>));
            RemoveDescriptorsByImplPartialName(services, "DbContextPool");

            // Both contexts use non-generic DbContextOptions constructor (via DbContextBase).
            // Register separate typed options that share the same in-memory DB.
            var trxnOptions = new DbContextOptionsBuilder<TaskFlowDbContextTrxn>()
                .UseInMemoryDatabase(_dbName).Options;
            var queryOptions = new DbContextOptionsBuilder<TaskFlowDbContextQuery>()
                .UseInMemoryDatabase(_dbName).Options;

            services.AddScoped(_ => CreateContext<TaskFlowDbContextTrxn>(trxnOptions));
            services.AddScoped(_ => CreateContext<TaskFlowDbContextQuery>(queryOptions));

            // Provide IDbContextFactory for DbContextScopedFactory
            services.AddSingleton<IDbContextFactory<TaskFlowDbContextTrxn>>(
                new TestDbContextFactory<TaskFlowDbContextTrxn>(trxnOptions));
            services.AddSingleton<IDbContextFactory<TaskFlowDbContextQuery>>(
                new TestDbContextFactory<TaskFlowDbContextQuery>(queryOptions));
        });
    }

    private static void RemoveDescriptorsByServiceType(IServiceCollection services, Type serviceType)
    {
        var descriptors = services.Where(d => d.ServiceType == serviceType).ToList();
        foreach (var descriptor in descriptors)
            services.Remove(descriptor);
    }

    private static void RemoveDescriptorsByImplPartialName(IServiceCollection services, string partialName)
    {
        var descriptors = services
            .Where(d => d.ImplementationType?.FullName?.Contains(partialName) == true
                     || d.ServiceType.FullName?.Contains(partialName) == true)
            .ToList();
        foreach (var descriptor in descriptors)
            services.Remove(descriptor);
    }

    internal static TContext CreateContext<TContext>(DbContextOptions options)
        where TContext : DbContext
    {
        // Try generic options constructor first, then non-generic
        var genericOptionsType = typeof(DbContextOptions<>).MakeGenericType(typeof(TContext));
        var ctor = typeof(TContext).GetConstructor([genericOptionsType])
                ?? typeof(TContext).GetConstructor([typeof(DbContextOptions)]);
        return (TContext)ctor!.Invoke([options]);
    }
}

internal sealed class TestDbContextFactory<TContext>(DbContextOptions options)
    : IDbContextFactory<TContext> where TContext : DbContext
{
    public TContext CreateDbContext() => CustomApiFactory.CreateContext<TContext>(options);
}
