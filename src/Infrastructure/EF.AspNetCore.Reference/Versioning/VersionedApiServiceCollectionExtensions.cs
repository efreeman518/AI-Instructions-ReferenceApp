using Asp.Versioning;
using Microsoft.Extensions.DependencyInjection;

namespace EF.AspNetCore.Versioning;

public static class VersionedApiServiceCollectionExtensions
{
    public static IServiceCollection AddEfVersionedOpenApi(
        this IServiceCollection services,
        Action<VersionedApiOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);

        var options = new VersionedApiOptions();
        configure(options);
        _ = options.DefaultDocument;

        services.AddSingleton(options);

        services.AddApiVersioning(versioning =>
        {
            versioning.DefaultApiVersion = options.DefaultDocument.Version;
            versioning.AssumeDefaultVersionWhenUnspecified = options.AssumeDefaultVersionWhenUnspecified;
            versioning.ReportApiVersions = true;
            versioning.ApiVersionReader = new UrlSegmentApiVersionReader();
        })
        .AddApiExplorer(apiExplorer =>
        {
            apiExplorer.GroupNameFormat = options.ApiExplorerGroupNameFormat;
            apiExplorer.SubstituteApiVersionInUrl = true;
        });

        if (!options.EnableOpenApi)
            return services;

        foreach (var document in options.Documents)
        {
            services.AddOpenApi(document.GroupName, openApi =>
            {
                openApi.ShouldInclude = apiDescription =>
                    string.Equals(apiDescription.GroupName, document.GroupName, StringComparison.OrdinalIgnoreCase);

                openApi.AddDocumentTransformer((openApiDocument, _, _) =>
                {
                    openApiDocument.Info = new()
                    {
                        Title = options.Title,
                        Version = document.DisplayName,
                        Description = options.Description
                    };
                    return Task.CompletedTask;
                });
            });
        }

        return services;
    }
}
