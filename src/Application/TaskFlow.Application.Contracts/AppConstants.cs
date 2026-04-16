namespace TaskFlow.Application.Contracts;

public static class AppConstants
{
    public const string DEFAULT_TIMEZONE = "America/New_York";
    public const int CACHE_PROVIDER_DEFAULT_DURATION_SECONDS = 60 * 60; // 1 hour
    public const string ROLE_GLOBAL_ADMIN = "GlobalAdmin";
    public const string ROLE_TENANT_ADMIN = "TenantAdmin";
    public const string ROLE_TENANT_MEMBER = "TenantMember";
    public const string DEFAULT_CACHE = "TaskFlowCache";
    public const string DEFAULT_DATETIME_FORMAT = "yyyy-MM-ddTHH:mm";
}
