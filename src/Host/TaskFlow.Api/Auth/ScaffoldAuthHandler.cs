using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace TaskFlow.Api.Auth;

/// <summary>
/// Scaffold-mode authentication handler that succeeds all requests with a predictable test identity.
/// TODO: [CONFIGURE] Remove or gate this handler when deploying with a real identity provider.
/// </summary>
public sealed class ScaffoldAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string SchemeName = "Scaffold";
    public const string ScaffoldUserId = "scaffold-user";
    public const string ScaffoldTenantId = "00000000-0000-0000-0000-000000000001";

    public ScaffoldAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder) { }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var claims = new[]
        {
            new Claim("oid", ScaffoldUserId),
            new Claim(ClaimTypes.NameIdentifier, ScaffoldUserId),
            new Claim(ClaimTypes.Name, "Scaffold Principal"),
            new Claim("tenant_id", ScaffoldTenantId),
            new Claim(ClaimTypes.Role, "GlobalAdmin"),
            new Claim(ClaimTypes.Role, "TenantAdmin"),
            new Claim(ClaimTypes.Role, "TenantMember"),
        };
        var identity = new ClaimsIdentity(claims, SchemeName);
        var ticket = new AuthenticationTicket(new ClaimsPrincipal(identity), SchemeName);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
