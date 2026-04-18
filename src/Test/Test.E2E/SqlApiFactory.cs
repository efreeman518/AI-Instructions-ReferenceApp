using EF.Data;
using EF.Data.Interceptors;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using TaskFlow.Infrastructure.Data;
using Testcontainers.MsSql;

namespace Test.E2E;

/// <summary>
/// WebApplicationFactory backed by a real SQL Server via Testcontainers.
/// Exercises the full stack: HTTP → Endpoint → Service → EF → SQL.
/// </summary>
public class SqlApiFactory : WebApplicationFactory<Program>
{
    private static MsSqlContainer _container = null!;
    private static string _connectionString = null!;
    private static bool _started;

    public static async Task StartContainerAsync()
    {
        if (_started) return;
        _container = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest").Build();
        await _container.StartAsync();
        _connectionString = _container.GetConnectionString();
        _started = true;
    }

    public static async Task StopContainerAsync()
    {
        if (!_started) return;
        await _container.DisposeAsync();
        _started = false;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.ConfigureServices(services =>
        {
            // Remove hosted services (background workers, Service Bus triggers)
            services.RemoveAll<IHostedService>();

            // Remove interceptors that need infrastructure not available in tests
            services.RemoveAll<AuditInterceptor<string, Guid?>>();
            services.RemoveAll<ConnectionNoLockInterceptor>();

            // Remove all EF registrations (pools, factories, contexts, options)
            RemoveDescriptors(services, typeof(TaskFlowDbContextTrxn));
            RemoveDescriptors(services, typeof(TaskFlowDbContextQuery));
            RemoveDescriptors(services, typeof(DbContextOptions<TaskFlowDbContextTrxn>));
            RemoveDescriptors(services, typeof(DbContextOptions<TaskFlowDbContextQuery>));
            RemoveDescriptors(services, typeof(DbContextOptions));
            RemoveDescriptors(services, typeof(IDbContextFactory<TaskFlowDbContextTrxn>));
            RemoveDescriptors(services, typeof(IDbContextFactory<TaskFlowDbContextQuery>));
            RemoveDescriptors(services, typeof(DbContextScopedFactory<TaskFlowDbContextTrxn, string, Guid?>));
            RemoveDescriptors(services, typeof(DbContextScopedFactory<TaskFlowDbContextQuery, string, Guid?>));
            RemoveByImplName(services, "DbContextPool");

            // Register real SQL Server contexts
            var trxnOptions = new DbContextOptionsBuilder<TaskFlowDbContextTrxn>()
                .UseSqlServer(_connectionString).Options;
            var queryOptions = new DbContextOptionsBuilder<TaskFlowDbContextQuery>()
                .UseSqlServer(_connectionString).Options;

            services.AddScoped(_ => CreateCtx<TaskFlowDbContextTrxn>(trxnOptions));
            services.AddScoped(_ => CreateCtx<TaskFlowDbContextQuery>(queryOptions));
            services.AddSingleton<IDbContextFactory<TaskFlowDbContextTrxn>>(
                new SqlTestDbContextFactory<TaskFlowDbContextTrxn>(trxnOptions));
            services.AddSingleton<IDbContextFactory<TaskFlowDbContextQuery>>(
                new SqlTestDbContextFactory<TaskFlowDbContextQuery>(queryOptions));
        });
    }

    private static void RemoveDescriptors(IServiceCollection services, Type serviceType)
    {
        foreach (var d in services.Where(d => d.ServiceType == serviceType).ToList())
            services.Remove(d);
    }

    private static void RemoveByImplName(IServiceCollection services, string partial)
    {
        foreach (var d in services.Where(d =>
            d.ImplementationType?.FullName?.Contains(partial) == true
            || d.ServiceType.FullName?.Contains(partial) == true).ToList())
            services.Remove(d);
    }

    internal static TContext CreateCtx<TContext>(DbContextOptions options) where TContext : DbContext
    {
        var genericType = typeof(DbContextOptions<>).MakeGenericType(typeof(TContext));
        var ctor = typeof(TContext).GetConstructor([genericType])
                ?? typeof(TContext).GetConstructor([typeof(DbContextOptions)]);
        return (TContext)ctor!.Invoke([options]);
    }
}

internal sealed class SqlTestDbContextFactory<TContext>(DbContextOptions options)
    : IDbContextFactory<TContext> where TContext : DbContext
{
    public TContext CreateDbContext() => SqlApiFactory.CreateCtx<TContext>(options);
}
