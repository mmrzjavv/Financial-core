using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Services.CoreService.Core.Persistence.Identity;

namespace Services.CoreService.Core.Persistence.Identity.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddIdentityPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString =
            configuration.GetConnectionString("PanelConnection")
            ?? configuration.GetConnectionString("DefaultConnection")
            ?? configuration.GetConnectionString("Postgres")
            ?? throw new InvalidOperationException(
                "Database connection string 'PanelConnection', 'DefaultConnection', or 'Postgres' is missing from configuration.");

        services.AddDbContext<PanelContext>(options => options.UseNpgsql(connectionString));
        return services;
    }
}
