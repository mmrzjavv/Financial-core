using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Services.CoreService.Core.Persistence.Identity;

public class PanelContextFactory : IDesignTimeDbContextFactory<PanelContext>
{
    public PanelContext CreateDbContext(string[] args)
    {
        // Design-time factory so `dotnet ef` doesn't need to build the full host (and all DI services).
        var basePath = Directory.GetCurrentDirectory();
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Path.Combine(basePath, "Api"))
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
            .AddEnvironmentVariables()
            .Build();

        var connectionString =
            GetConnectionStringFromArgs(args)
            ?? configuration.GetConnectionString("PanelConnection")
            ?? configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("No connection string found (PanelConnection/DefaultConnection).");

        var optionsBuilder = new DbContextOptionsBuilder<PanelContext>();
        optionsBuilder.UseNpgsql(connectionString);

        return new PanelContext(optionsBuilder.Options);
    }

    private static string? GetConnectionStringFromArgs(string[] args)
    {
        for (var index = 0; index < args.Length - 1; index++)
        {
            if (!string.Equals(args[index], "--connection", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var rawValue = args[index + 1];
            return string.IsNullOrWhiteSpace(rawValue)
                ? null
                : rawValue.Trim().Trim('"');
        }

        return null;
    }
}
