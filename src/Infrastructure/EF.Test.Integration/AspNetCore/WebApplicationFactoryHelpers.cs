using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EF.Test.Integration.AspNetCore;

public static class WebApplicationFactoryHelpers
{
    public static TContext CreateContext<TContext>(DbContextOptions options) where TContext : DbContext
    {
        ArgumentNullException.ThrowIfNull(options);

        var genericOptionsType = typeof(DbContextOptions<>).MakeGenericType(typeof(TContext));
        var ctor = typeof(TContext).GetConstructor([genericOptionsType])
                ?? typeof(TContext).GetConstructor([typeof(DbContextOptions)]);

        return ctor is null
            ? throw new InvalidOperationException(
                $"{typeof(TContext).FullName} must expose a constructor accepting DbContextOptions<{typeof(TContext).Name}> or DbContextOptions.")
            : (TContext)ctor.Invoke([options]);
    }

    public static void RemoveDescriptorsByServiceType(this IServiceCollection services, Type serviceType)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(serviceType);

        foreach (var descriptor in services.Where(d => d.ServiceType == serviceType).ToList())
            services.Remove(descriptor);
    }

    public static void RemoveDescriptorsByImplementationPartialName(this IServiceCollection services, string partialName)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrWhiteSpace(partialName);

        foreach (var descriptor in services.Where(d =>
            d.ImplementationType?.FullName?.Contains(partialName, StringComparison.Ordinal) == true
            || d.ServiceType.FullName?.Contains(partialName, StringComparison.Ordinal) == true).ToList())
        {
            services.Remove(descriptor);
        }
    }

    public static void RemoveDescriptorsByServiceTypeFullName(this IServiceCollection services, string fullName)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrWhiteSpace(fullName);

        foreach (var descriptor in services.Where(d => d.ServiceType.FullName == fullName).ToList())
            services.Remove(descriptor);
    }
}
