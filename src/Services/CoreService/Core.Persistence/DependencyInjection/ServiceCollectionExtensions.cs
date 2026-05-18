using BuildingBlocks.Application.Abstractions;
using BuildingBlocks.Application.Common;
using BuildingBlocks.Persistence.Db.DomainEvents;
using BuildingBlocks.Persistence.Db.Interceptors;
using Core.Application.Abstractions;
using Core.Persistence.Interceptors;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Core.Persistence.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCorePersistence(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IDomainEventDispatcher, MediatRDomainEventDispatcher>();
        services.AddScoped<IClock, SystemClock>();

        services.AddScoped<AuditingSaveChangesInterceptor>();
        services.AddScoped<SoftDeleteSaveChangesInterceptor>();
        services.AddScoped<InvestmentCaseUpdateSuppressorInterceptor>();

        var connectionString = configuration.GetConnectionString("Postgres")
            ?? throw new InvalidOperationException(SystemMessages.PostgresConnectionMissing);

        services.AddDbContext<CoreDbContext>((sp, options) =>
        {
            options.UseNpgsql(connectionString, npgsql =>
            {
                npgsql.EnableRetryOnFailure(5);
            });

            options.AddInterceptors(
                sp.GetRequiredService<AuditingSaveChangesInterceptor>(),
                sp.GetRequiredService<SoftDeleteSaveChangesInterceptor>(),
                sp.GetRequiredService<InvestmentCaseUpdateSuppressorInterceptor>());
        });

        services.AddScoped<ICoreDbContext>(sp => sp.GetRequiredService<CoreDbContext>());

        return services;
    }
}

