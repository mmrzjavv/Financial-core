namespace Core.API.DependencyInjection;

public static class HostConfigurationExtensions
{
    public static WebApplicationBuilder ConfigureHostUrls(this WebApplicationBuilder builder)
    {
        var coreUrls = Environment.GetEnvironmentVariable("CORE_URLS");
        var aspnetcoreUrls = Environment.GetEnvironmentVariable("ASPNETCORE_URLS");
        if (!string.IsNullOrWhiteSpace(coreUrls) && string.IsNullOrWhiteSpace(aspnetcoreUrls))
        {
            builder.WebHost.UseUrls(coreUrls.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
        }

        return builder;
    }
}
