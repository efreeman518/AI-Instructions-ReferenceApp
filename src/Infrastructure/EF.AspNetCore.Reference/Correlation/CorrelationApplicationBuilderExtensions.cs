namespace EF.AspNetCore.Correlation;

using Microsoft.AspNetCore.Builder;

public static class CorrelationApplicationBuilderExtensions
{
    public static IApplicationBuilder UseCorrelationId(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);
        return app.UseMiddleware<CorrelationIdMiddleware>();
    }
}
