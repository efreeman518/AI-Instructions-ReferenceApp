namespace EF.AspNetCore.Security;

using Microsoft.AspNetCore.Builder;

public static class SecurityHeadersApplicationBuilderExtensions
{
    public static IApplicationBuilder UseBasicSecurityHeaders(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);
        return app.UseMiddleware<SecurityHeadersMiddleware>();
    }
}
