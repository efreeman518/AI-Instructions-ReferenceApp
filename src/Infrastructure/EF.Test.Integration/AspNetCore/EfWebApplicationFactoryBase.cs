using EF.Data;
using EF.Data.Interceptors;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace EF.Test.Integration.AspNetCore;

public abstract class EfWebApplicationFactoryBase<TProgram, TTrxnContext, TQueryContext>
    : WebApplicationFactory<TProgram>
    where TProgram : class
    where TTrxnContext : DbContextBase<string, Guid?>
    where TQueryContext : DbContextBase<string, Guid?>
{
    protected virtual string EnvironmentName => Environments.Development;
    protected virtual bool RemoveHostedServices => true;
    protected virtual string? StartupTaskServiceTypeFullName => null;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(EnvironmentName);
        builder.ConfigureAppConfiguration((_, config) => ConfigureTestConfiguration(config));

        builder.ConfigureServices(services =>
        {
            if (RemoveHostedServices)
                services.RemoveAll<IHostedService>();

            RemoveStandardEfInfrastructure(services);
            RemoveStartupTasks(services);
            RemoveAppSpecificServices(services);

            var trxnOptions = BuildTrxnOptions();
            var queryOptions = BuildQueryOptions();

            services.AddScoped(_ => WebApplicationFactoryHelpers.CreateContext<TTrxnContext>(trxnOptions));
            services.AddScoped(_ => WebApplicationFactoryHelpers.CreateContext<TQueryContext>(queryOptions));

            services.AddSingleton<IDbContextFactory<TTrxnContext>>(
                new EfTestDbContextFactory<TTrxnContext>(trxnOptions));
            services.AddSingleton<IDbContextFactory<TQueryContext>>(
                new EfTestDbContextFactory<TQueryContext>(queryOptions));

            ConfigureAdditionalTestServices(services);
        });
    }

    protected abstract DbContextOptions BuildTrxnOptions();

    protected abstract DbContextOptions BuildQueryOptions();

    protected virtual void ConfigureTestConfiguration(IConfigurationBuilder config) { }

    protected virtual void ConfigureAdditionalTestServices(IServiceCollection services) { }

    protected virtual void RemoveAppSpecificServices(IServiceCollection services) { }

    private static void RemoveStandardEfInfrastructure(IServiceCollection services)
    {
        services.RemoveAll<AuditInterceptor<string, Guid?>>();
        services.RemoveAll<ConnectionNoLockInterceptor>();

        services.RemoveDescriptorsByServiceType(typeof(TTrxnContext));
        services.RemoveDescriptorsByServiceType(typeof(TQueryContext));
        services.RemoveDescriptorsByServiceType(typeof(DbContextOptions<TTrxnContext>));
        services.RemoveDescriptorsByServiceType(typeof(DbContextOptions<TQueryContext>));
        services.RemoveDescriptorsByServiceType(typeof(DbContextOptions));
        services.RemoveDescriptorsByServiceType(typeof(IDbContextFactory<TTrxnContext>));
        services.RemoveDescriptorsByServiceType(typeof(IDbContextFactory<TQueryContext>));
        services.RemoveDescriptorsByServiceType(typeof(DbContextScopedFactory<TTrxnContext, string, Guid?>));
        services.RemoveDescriptorsByServiceType(typeof(DbContextScopedFactory<TQueryContext, string, Guid?>));
        services.RemoveDescriptorsByImplementationPartialName("DbContextPool");
    }

    private void RemoveStartupTasks(IServiceCollection services)
    {
        if (!string.IsNullOrWhiteSpace(StartupTaskServiceTypeFullName))
            services.RemoveDescriptorsByServiceTypeFullName(StartupTaskServiceTypeFullName);
    }
}
