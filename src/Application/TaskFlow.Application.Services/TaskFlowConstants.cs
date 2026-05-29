namespace TaskFlow.Application.Services;

/// <summary>Provides task flow constants behavior for the Application layer.</summary>
internal static class TaskFlowConstants
{
    // Placeholder tenant ID until Phase 5f (Auth) wires request context
    public static readonly Guid DefaultTenantId = Guid.Parse("00000000-0000-0000-0000-000000000001");
}
