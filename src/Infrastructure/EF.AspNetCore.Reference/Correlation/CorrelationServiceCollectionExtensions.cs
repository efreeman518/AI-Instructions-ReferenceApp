using Microsoft.Extensions.DependencyInjection;

namespace EF.AspNetCore.Correlation;

public static class CorrelationServiceCollectionExtensions
{
    public static IServiceCollection AddCorrelationHeaderPropagation(
        this IServiceCollection services,
        string headerName = CorrelationIdOptions.DefaultHeaderName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(headerName);

        services.Configure<CorrelationIdSettings>(settings =>
        {
            settings.HeaderName = headerName;
        });

        services.AddHeaderPropagation(options =>
        {
            options.Headers.Add(headerName);
        });

        return services;
    }
}
