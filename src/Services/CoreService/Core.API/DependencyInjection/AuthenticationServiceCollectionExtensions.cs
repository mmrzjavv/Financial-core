using BuildingBlocks.Application.Common;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace Core.API.DependencyInjection;

public static class AuthenticationServiceCollectionExtensions
{
    public static IServiceCollection AddCoreAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
        {
            var jwtKey = configuration["JwtKey"] ?? throw new InvalidOperationException(SystemMessages.JwtKeyMissing);
            var encKey = configuration["ENCKey"] ?? throw new InvalidOperationException(SystemMessages.EncKeyMissing);
            options.RequireHttpsMetadata = false;
            options.SaveToken = true;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = false,
                ValidateAudience = false,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
                TokenDecryptionKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(encKey)),
                ClockSkew = TimeSpan.Zero
            };
        });

        return services;
    }
}
