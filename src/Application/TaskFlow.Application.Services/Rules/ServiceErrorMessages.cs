namespace TaskFlow.Application.Services.Rules;

public static class ServiceErrorMessages
{
    public static string PayloadRequired(string entityName) => $"{entityName} payload is required.";
    public static string NameRequired(string entityName) => $"Name is required for {entityName}.";
    public static string ItemNotFound(string entityName, Guid id) => $"{entityName} not found: {id}";
    public static string TenantMismatch(string label) => $"Cannot add child because it belongs to a different tenant: {label}.";
    public static string CycleDetected(string label) => $"Cannot add child because it would create a cycle: {label}.";
    public static string SelfReferenceNotAllowed(string entityName) => $"A {entityName} cannot be a child of itself.";
    public static string TenantNotFound(Guid tenantId) => $"Tenant with ID {tenantId} not found.";
}
