using System.Security.Claims;
using EF.Common.Contracts;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace TaskFlow.Bootstrapper;

public static partial class RegisterServices
{
    private static void AddRequestContext(IServiceCollection services)
    {
        services.AddHttpContextAccessor();

        services.AddScoped<IRequestContext<string, Guid?>>(sp =>
        {
            var httpContextAccessor = sp.GetRequiredService<IHttpContextAccessor>();
            var user = httpContextAccessor.HttpContext?.User;

            if (user?.Identity?.IsAuthenticated != true)
            {
                return new RequestContext<string, Guid?>(
                    "system",
                    Guid.NewGuid().ToString(),
                    Guid.Parse("00000000-0000-0000-0000-000000000001"),
                    new List<string>());
            }

            var userId = user.FindFirst("oid")?.Value
                      ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value
                      ?? user.FindFirst("sub")?.Value
                      ?? "unknown";

            var correlationId = httpContextAccessor.HttpContext?.Request.Headers["X-Correlation-Id"].FirstOrDefault()
                             ?? Guid.NewGuid().ToString();

            var tenantClaim = user.FindFirst("tenant_id")?.Value;
            Guid? tenantId = Guid.TryParse(tenantClaim, out var tid) ? tid : null;

            var roles = user.FindAll(ClaimTypes.Role)
                           .Select(c => c.Value)
                           .ToList();

            return new RequestContext<string, Guid?>(userId, correlationId, tenantId, roles);
        });
    }
}
