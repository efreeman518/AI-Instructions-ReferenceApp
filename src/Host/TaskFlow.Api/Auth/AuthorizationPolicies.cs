using TaskFlow.Application.Contracts;
using Microsoft.AspNetCore.Authorization;

namespace TaskFlow.Api.Auth;

/// <summary>Configures authorization policies host behavior for TaskFlow runtime services.</summary>
public static class AuthorizationPolicies
{
    public const string TenantMatch = "TenantMatch";
    public const string TenantAdmin = "TenantAdmin";
    public const string GlobalAdmin = "GlobalAdmin";
    public const string StatusTransition = "StatusTransition";
    private const string FlowEngineAdmin = "FlowEngine.Admin";
    private const string FlowEngineHumanTask = "FlowEngine.HumanTask";

    /// <summary>Registers task flow authorization dependencies in the service container.</summary>
    public static IServiceCollection AddTaskFlowAuthorization(this IServiceCollection services)
    {
        var authenticatedUserPolicy = new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .Build();

        services.AddAuthorizationBuilder()
            .SetFallbackPolicy(authenticatedUserPolicy)
            .SetDefaultPolicy(authenticatedUserPolicy)
            .AddPolicy(GlobalAdmin, policy =>
                policy.RequireRole(AppConstants.ROLE_GLOBAL_ADMIN))
            .AddPolicy(FlowEngineAdmin, policy =>
                policy.RequireRole(AppConstants.ROLE_GLOBAL_ADMIN))
            .AddPolicy(FlowEngineHumanTask, policy =>
                policy.RequireAssertion(context =>
                    context.User.IsInRole(AppConstants.ROLE_GLOBAL_ADMIN)
                    || context.User.IsInRole("TenantAdmin")
                    || context.User.IsInRole("TenantMember")))
            .AddPolicy(TenantMatch, policy =>
                policy.RequireAuthenticatedUser()
                      .RequireAssertion(context =>
                      {
                          // GlobalAdmin bypasses tenant check
                          if (context.User.IsInRole(AppConstants.ROLE_GLOBAL_ADMIN))
                              return true;

                          // TenantMember or TenantAdmin required, plus tenant_id claim must be present
                          var hasTenantRole = context.User.IsInRole("TenantMember")
                                          || context.User.IsInRole("TenantAdmin");
                          var hasTenantClaim = context.User.HasClaim(c => c.Type == "tenant_id");
                          return hasTenantRole && hasTenantClaim;
                      }))
            .AddPolicy(TenantAdmin, policy =>
                policy.RequireAuthenticatedUser()
                      .RequireAssertion(context =>
                          context.User.IsInRole(AppConstants.ROLE_GLOBAL_ADMIN)
                          || context.User.IsInRole("TenantAdmin")))
            .AddPolicy(StatusTransition, policy =>
                policy.RequireAuthenticatedUser()
                      .RequireAssertion(context =>
                          context.User.IsInRole(AppConstants.ROLE_GLOBAL_ADMIN)
                          || context.User.IsInRole("TenantAdmin")
                          || context.User.IsInRole("TenantMember")));

        return services;
    }
}
