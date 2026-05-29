namespace TaskFlow.Application.Cqrs.Shared;

/// <summary>Provides service error messages behavior for the Features Shared layer.</summary>
public static class ServiceErrorMessages
{
    /// <summary>Provides the payload required operation for service error messages.</summary>
    public static string PayloadRequired(string entityName) => $"{entityName} payload is required.";
    /// <summary>Provides the name required operation for service error messages.</summary>
    public static string NameRequired(string entityName) => $"Name is required for {entityName}.";
    /// <summary>Provides the item not found operation for service error messages.</summary>
    public static string ItemNotFound(string entityName, Guid id) => $"{entityName} not found: {id}";
    /// <summary>Provides the tenant mismatch operation for service error messages.</summary>
    public static string TenantMismatch(string label) => $"Cannot add child because it belongs to a different tenant: {label}.";
    /// <summary>Provides the cycle detected operation for service error messages.</summary>
    public static string CycleDetected(string label) => $"Cannot add child because it would create a cycle: {label}.";
    /// <summary>Provides the self reference not allowed operation for service error messages.</summary>
    public static string SelfReferenceNotAllowed(string entityName) => $"A {entityName} cannot be a child of itself.";
    /// <summary>Provides the tenant not found operation for service error messages.</summary>
    public static string TenantNotFound(Guid tenantId) => $"Tenant with ID {tenantId} not found.";
}
