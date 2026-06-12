using System.Net;
using System.Text;
using Asp.Versioning;
using BuildingBlocks.Application.Results;
using BuildingBlocks.Observability.DependencyInjection;
using Core.API.Authorization;
using Core.API.Http;
using Core.API.Swagger;
using Core.Application;
using Core.Application.Common;
using Core.Domain.Identity;
using Mapster;
using MapsterMapper;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.IdentityModel.Tokens;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace Core.API.DependencyInjection;

public static class ApiPresentationServiceCollectionExtensions
{
    public static IServiceCollection AddCoreApiPresentation(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddCors(options =>
        {
            options.AddPolicy("CorsPolicy", policy =>
            {
                policy
                    .AllowAnyOrigin()
                    .AllowAnyHeader()
                    .AllowAnyMethod();
            });
        });

        services.AddPlatformObservability();

        services.AddApiVersioning(options =>
        {
            options.DefaultApiVersion = new ApiVersion(1, 0);
            options.AssumeDefaultVersionWhenUnspecified = true;
            options.ReportApiVersions = true;
            options.ApiVersionReader = ApiVersionReader.Combine(new UrlSegmentApiVersionReader(), new HeaderApiVersionReader("X-Api-Version"));
        }).AddApiExplorer(options =>
        {
            options.GroupNameFormat = "'v'VVV";
            options.SubstituteApiVersionInUrl = true;
        });

        services.AddControllers()
            .ConfigureApiBehaviorOptions(options =>
            {
                options.InvalidModelStateResponseFactory = context =>
                {
                    var validationErrors = context.ModelState
                        .Where(entry => entry.Value?.Errors.Count > 0)
                        .ToDictionary(
                            entry => entry.Key,
                            entry => entry.Value!.Errors.Select(error => error.ErrorMessage).ToArray());

                    var envelope = new ApiOperationResult<object?>().Failed(
                        ApiMessages.ValidationFailed,
                        validationErrors,
                        HttpStatusCode.BadRequest);

                    return ApiResponse.Send(envelope);
                };
            });

        services.AddEndpointsApiExplorer();
        services.AddCoreSwagger();

        services.AddOpenTelemetry()
            .ConfigureResource(r => r.AddService(serviceName: "core-service"))
            .WithTracing(tracing =>
            {
                tracing
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddOtlpExporter();
            });

        services.AddHealthChecks();
        services.Configure<FormOptions>(o => o.MultipartBodyLengthLimit = 250L * 1024 * 1024);

        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = configuration.GetConnectionString("Redis");
        });

        TypeAdapterConfig.GlobalSettings.Scan(typeof(ApplicationMapsterConfig).Assembly, typeof(ApplicationServiceCollectionExtensions).Assembly);
        services.AddSingleton(TypeAdapterConfig.GlobalSettings);
        services.AddScoped<IMapper, ServiceMapper>();

        return services;
    }
}
