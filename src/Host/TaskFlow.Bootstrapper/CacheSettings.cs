namespace TaskFlow.Bootstrapper;

public class CacheSettings
{
    public string Name { get; set; } = "Default";
    public int DurationMinutes { get; set; } = 30;
    public int DistributedCacheDurationMinutes { get; set; } = 60;
    public int FailSafeMaxDurationMinutes { get; set; } = 120;
    public int FailSafeThrottleDurationSeconds { get; set; } = 1;
    public string? RedisConnectionStringName { get; set; }
    public string? BackplaneChannelName { get; set; }
}
