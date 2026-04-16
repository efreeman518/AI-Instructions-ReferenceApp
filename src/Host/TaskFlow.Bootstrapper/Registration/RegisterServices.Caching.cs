using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TaskFlow.Application.Contracts;
using ZiggyCreatures.Caching.Fusion;
using ZiggyCreatures.Caching.Fusion.Backplane.StackExchangeRedis;
using ZiggyCreatures.Caching.Fusion.Serialization.SystemTextJson;

namespace TaskFlow.Bootstrapper;

public static partial class RegisterServices
{
    private static void AddCachingServices(IServiceCollection services, IConfiguration config)
    {
        List<CacheSettings> cacheSettings = [];
        config.GetSection("CacheSettings").Bind(cacheSettings);

        if (cacheSettings.Count == 0)
        {
            cacheSettings.Add(new CacheSettings { Name = AppConstants.DEFAULT_CACHE });
        }

        foreach (var settings in cacheSettings)
        {
            var fcBuilder = services.AddFusionCache(settings.Name)
                .WithSystemTextJsonSerializer(new JsonSerializerOptions
                {
                    ReferenceHandler = ReferenceHandler.Preserve
                })
                .WithCacheKeyPrefix($"{settings.Name}:")
                .WithDefaultEntryOptions(new FusionCacheEntryOptions
                {
                    Duration = TimeSpan.FromMinutes(settings.DurationMinutes),
                    DistributedCacheDuration = TimeSpan.FromMinutes(settings.DistributedCacheDurationMinutes),
                    IsFailSafeEnabled = true,
                    FailSafeMaxDuration = TimeSpan.FromMinutes(settings.FailSafeMaxDurationMinutes),
                    FailSafeThrottleDuration = TimeSpan.FromSeconds(settings.FailSafeThrottleDurationSeconds),
                    JitterMaxDuration = TimeSpan.FromSeconds(10),
                    FactorySoftTimeout = TimeSpan.FromSeconds(1),
                    FactoryHardTimeout = TimeSpan.FromSeconds(30),
                    EagerRefreshThreshold = 0.9f
                });

            var redisConnStr = !string.IsNullOrEmpty(settings.RedisConnectionStringName)
                ? config.GetConnectionString(settings.RedisConnectionStringName)
                : null;

            if (!string.IsNullOrEmpty(redisConnStr))
            {
                fcBuilder
                    .WithDistributedCache(new RedisCache(new RedisCacheOptions
                    {
                        Configuration = redisConnStr
                    }))
                    .WithBackplane(new RedisBackplane(new RedisBackplaneOptions
                    {
                        Configuration = redisConnStr
                    }));
            }
        }
    }
}
