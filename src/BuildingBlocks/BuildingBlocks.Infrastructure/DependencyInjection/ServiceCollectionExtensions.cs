using BuildingBlocks.Application.Abstractions;
using BuildingBlocks.Infrastructure.Json;
using BuildingBlocks.Infrastructure.Redis;
using BuildingBlocks.Infrastructure.S3;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace BuildingBlocks.Infrastructure.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddBuildingBlocksInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<IJsonSerializer, SystemTextJsonSerializer>();

        var s3Section = configuration.GetSection("S3");
        services.AddSingleton(Options.Create(new S3Options
        {
            ServiceUrl = s3Section["ServiceUrl"] ?? string.Empty,
            AccessKey = s3Section["AccessKey"] ?? string.Empty,
            SecretKey = s3Section["SecretKey"] ?? string.Empty,
            BucketName = s3Section["BucketName"] ?? string.Empty,
            ForcePathStyle = bool.TryParse(s3Section["ForcePathStyle"], out var forcePathStyle) ? forcePathStyle : true
        }));
        services.AddSingleton<IFileStorage, S3FileStorage>();

        var redisConn = configuration.GetConnectionString("Redis");
        if (!string.IsNullOrWhiteSpace(redisConn))
        {
            services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(redisConn));
            services.AddSingleton<ICache, RedisCache>();
        }

        return services;
    }
}
