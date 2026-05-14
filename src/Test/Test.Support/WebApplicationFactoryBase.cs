using EF.Data;
using EF.Data.Interceptors;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace Test.Support;

/// <summary>
/// Shared base for WebApplicationFactory-based test factories.
///
/// Subclasses override <see cref="BuildTrxnOptions"/> and <see cref="BuildQueryOptions"/> to choose the test
/// database (in-memory for endpoint contract tests, Testcontainers SQL for E2E workflow tests).
///
/// Removes the standard pooled-DbContext + interceptor + scoped-factory plumbing that the production host
/// registers, then re-registers test-mode contexts using <see cref="TestDbContextFactory{TContext}"/>.
///
/// Constrained to <c>DbContextBase&lt;string, Guid?&gt;</c> — the EF.Packages canonical audit/tenant shape.
/// Apps that deviate from these types must override <see cref="ConfigureWebHost"/> directly rather than using
/// this base.
/// </summary>
public abstract class WebApplicationFactoryBase<TProgram, TTrxnContext, TQueryContext> : WebApplicationFactory<TProgram>
    where TProgram : class
    where TTrxnContext : DbContextBase<string, Guid?>
    where TQueryContext : DbContextBase<string, Guid?>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IHostedService>();

            RemoveStandardEfInfrastructure(services);
            RemoveStartupTasks(services);
            RemoveAppSpecificServices(services);

            var trxnOptions = BuildTrxnOptions();
            var queryOptions = BuildQueryOptions();

            services.AddScoped(_ => WebApplicationFactoryHelpers.CreateContext<TTrxnContext>(trxnOptions));
            services.AddScoped(_ => WebApplicationFactoryHelpers.CreateContext<TQueryContext>(queryOptions));

            services.AddSingleton<IDbContextFactory<TTrxnContext>>(
                new TestDbContextFactory<TTrxnContext>(trxnOptions));
            services.AddSingleton<IDbContextFactory<TQueryContext>>(
                new TestDbContextFactory<TQueryContext>(queryOptions));
        });
    }

    /// <summary>Build the transactional context options. Override with `UseInMemoryDatabase(...)` or `UseSqlServer(...)`.</summary>
    protected abstract DbContextOptions BuildTrxnOptions();

    /// <summary>Build the query context options. Typically shares the same connection as Trxn.</summary>
    protected abstract DbContextOptions BuildQueryOptions();

    /// <summary>Hook for removing app-specific services (extra interceptors, additional pooled contexts, etc.).
    /// Default implementation is a no-op.</summary>
    protected virtual void RemoveAppSpecificServices(IServiceCollection services) { }

    private static void RemoveStandardEfInfrastructure(IServiceCollection services)
    {
        services.RemoveAll<AuditInterceptor<string, Guid?>>();
        services.RemoveAll<ConnectionNoLockInterceptor>();

        WebApplicationFactoryHelpers.RemoveDescriptorsByServiceType(services, typeof(TTrxnContext));
        WebApplicationFactoryHelpers.RemoveDescriptorsByServiceType(services, typeof(TQueryContext));
        WebApplicationFactoryHelpers.RemoveDescriptorsByServiceType(services, typeof(DbContextOptions<TTrxnContext>));
        WebApplicationFactoryHelpers.RemoveDescriptorsByServiceType(services, typeof(DbContextOptions<TQueryContext>));
        WebApplicationFactoryHelpers.RemoveDescriptorsByServiceType(services, typeof(DbContextOptions));
        WebApplicationFactoryHelpers.RemoveDescriptorsByServiceType(services, typeof(IDbContextFactory<TTrxnContext>));
        WebApplicationFactoryHelpers.RemoveDescriptorsByServiceType(services, typeof(IDbContextFactory<TQueryContext>));
        WebApplicationFactoryHelpers.RemoveDescriptorsByServiceType(services, typeof(DbContextScopedFactory<TTrxnContext, string, Guid?>));
        WebApplicationFactoryHelpers.RemoveDescriptorsByServiceType(services, typeof(DbContextScopedFactory<TQueryContext, string, Guid?>));
        WebApplicationFactoryHelpers.RemoveDescriptorsByImplPartialName(services, "DbContextPool");
    }

    private static void RemoveStartupTasks(IServiceCollection services)
    {
        WebApplicationFactoryHelpers.RemoveDescriptorsByServiceTypeFullName(
            services,
            "TaskFlow.Bootstrapper.IStartupTask");
    }
}

/// <summary>Test-mode <see cref="IDbContextFactory{TContext}"/> that creates contexts via reflection,
/// bypassing required-member enforcement on <c>DbContextBase</c>.</summary>
public sealed class TestDbContextFactory<TContext>(DbContextOptions options) : IDbContextFactory<TContext>
    where TContext : DbContext
{
    public TContext CreateDbContext() => WebApplicationFactoryHelpers.CreateContext<TContext>(options);
}

public static class WebApplicationFactoryHelpers
{
    public static TContext CreateContext<TContext>(DbContextOptions options) where TContext : DbContext
    {
        var genericOptionsType = typeof(DbContextOptions<>).MakeGenericType(typeof(TContext));
        var ctor = typeof(TContext).GetConstructor([genericOptionsType])
                ?? typeof(TContext).GetConstructor([typeof(DbContextOptions)]);
        return (TContext)ctor!.Invoke([options]);
    }

    public static void RemoveDescriptorsByServiceType(IServiceCollection services, Type serviceType)
    {
        foreach (var d in services.Where(d => d.ServiceType == serviceType).ToList())
            services.Remove(d);
    }

    public static void RemoveDescriptorsByImplPartialName(IServiceCollection services, string partialName)
    {
        foreach (var d in services.Where(d =>
            d.ImplementationType?.FullName?.Contains(partialName) == true
            || d.ServiceType.FullName?.Contains(partialName) == true).ToList())
            services.Remove(d);
    }

    public static void RemoveDescriptorsByServiceTypeFullName(IServiceCollection services, string fullName)
    {
        foreach (var d in services.Where(d => d.ServiceType.FullName == fullName).ToList())
            services.Remove(d);
    }
}
