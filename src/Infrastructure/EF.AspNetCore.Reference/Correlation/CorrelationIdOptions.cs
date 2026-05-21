namespace EF.AspNetCore.Correlation;

public static class CorrelationIdOptions
{
    public const string DefaultHeaderName = CorrelationIdMiddleware.DefaultHeaderName;
}

public sealed class CorrelationIdSettings
{
    public string HeaderName { get; set; } = CorrelationIdOptions.DefaultHeaderName;
}
