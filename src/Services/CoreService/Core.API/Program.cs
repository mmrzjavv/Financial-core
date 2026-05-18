using System.Net;
using System.Security.Claims;
using System.Text;
using Asp.Versioning;
using BuildingBlocks.Application.Common;
using BuildingBlocks.Application.Results;
using BuildingBlocks.Application.Validation;
using BuildingBlocks.Domain.Abstractions;
using BuildingBlocks.Observability.Correlation;
using BuildingBlocks.Observability.Logging;
using Core.API;
using Core.API.Authorization;
using Core.API.Http;
using Core.Application;
using Core.Application.Abstractions;
using Core.Application.Authorization;
using Core.Application.Common;
using Core.Application.Services;
using Core.Domain.Constants;
using Core.Infrastructure.DependencyInjection;
using Core.Infrastructure.Identity.DependencyInjection;
using Core.Workflow.DependencyInjection;
using FluentValidation;
using FluentValidation.AspNetCore;
using Mapster;
using MapsterMapper;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.IdentityModel.Tokens;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;


var builder = WebApplication.CreateBuilder(args);

ValidationLocalization.ConfigurePersian();

var coreUrls = Environment.GetEnvironmentVariable("CORE_URLS");
var aspnetcoreUrls = Environment.GetEnvironmentVariable("ASPNETCORE_URLS");
if (!string.IsNullOrWhiteSpace(coreUrls) && string.IsNullOrWhiteSpace(aspnetcoreUrls))
{
    builder.WebHost.UseUrls(coreUrls.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
}

builder.Host.UsePlatformSerilog();

builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton<CorrelationIdMiddleware>();
builder.Services.AddSingleton<IClock, SystemClock>();
builder.Services.AddScoped<IUserContext, HttpUserContext>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("CorsPolicy", policy =>
    {
        policy
            .AllowAnyOrigin()
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

builder.Services.AddValidatorsFromAssemblyContaining<InvestmentCaseAppService>();
builder.Services.AddFluentValidationAutoValidation();

builder.Services.AddScoped<ICaseStateManager, CaseStateManager>();
builder.Services.AddScoped<IInvestmentCaseAppService, InvestmentCaseAppService>();
builder.Services.AddScoped<IKanbanAppService, KanbanAppService>();
builder.Services.AddScoped<ICompanyAppService, CompanyAppService>();
builder.Services.AddScoped<ICaseAuthorizationService, CaseAuthorizationService>();
builder.Services.AddScoped<ICaseNumberGenerator, CaseNumberGenerator>();

builder.AddIdentityApplication();
builder.AddIdentityInfrastructure();

builder.Services.AddCoreInfrastructure(builder.Configuration);
builder.Services.AddCoreWorkflow(builder.Configuration, builder.Environment);

builder.Services.AddProblemDetails();

builder.Services.AddApiVersioning(options =>
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

builder.Services.AddControllers()
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

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "JWT Authorization header using the Bearer scheme."
    });
    options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

builder.Services.AddOpenTelemetry()
    .ConfigureResource(r => r.AddService(serviceName: "core-service"))
    .WithTracing(tracing =>
    {
        tracing
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddOtlpExporter();
    });

builder.Services.AddHealthChecks();
builder.Services.Configure<FormOptions>(o => { o.MultipartBodyLengthLimit = 250L * 1024 * 1024; });

builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("Redis");
});

TypeAdapterConfig.GlobalSettings.Scan(typeof(Program).Assembly, typeof(ApplicationMapsterConfig).Assembly);
builder.Services.AddSingleton(TypeAdapterConfig.GlobalSettings);
builder.Services.AddScoped<IMapper, ServiceMapper>();

builder.Services.AddAuthorization(options =>
{
    // IdentityService issues ClaimTypes.Role = UserRole.ToString(): Applicant, InvestmentExpert, InvestmentManager, LegalExpert, FinancialExpert, Admin, ...
    options.AddPolicy("ApplicantOnly", p => p.RequireRole(SystemRoles.Applicant, SystemRoles.Admin));
    options.AddPolicy("InternalOnly", p => p.RequireRole(
        SystemRoles.InvestmentExpert,
        SystemRoles.InvestmentManager,
        SystemRoles.FinancialExpert,
        SystemRoles.LegalExpert,
        SystemRoles.Ceo,
        SystemRoles.Admin,
        "LegalUnit",
        "FinancialUnit",
        "InvestmentUnit",
        "CEO"));

    options.AddPolicy("InvestmentCases.Review", p => p.Requirements.Add(new PermissionRequirement("investment_cases:review")));
    options.AddPolicy("InvestmentCases.FinanceReview", p => p.Requirements.Add(new PermissionRequirement("investment_cases:finance_review")));
    options.AddPolicy("InvestmentCases.LegalReview", p => p.Requirements.Add(new PermissionRequirement("investment_cases:legal_review")));
    // Role-based: permission cache can lag after role changes; CaseAuthorizationService enforces cases:ceo_approve in app layer.
    options.AddPolicy("InvestmentCases.CeoApprove", p => p.RequireRole(
        SystemRoles.Ceo,
        SystemRoles.Admin,
        "CEO"));

    options.AddPolicy("Dashboard.Ceo", p => p.RequireRole(SystemRoles.Ceo, SystemRoles.Admin, "CEO"));
    options.AddPolicy("Dashboard.Board", p => p.RequireRole(
        SystemRoles.Ceo,
        SystemRoles.InvestmentManager,
        SystemRoles.Admin,
        "CEO"));
});

builder.Services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
{
    var jwtKey = builder.Configuration["JwtKey"] ?? throw new InvalidOperationException(SystemMessages.JwtKeyMissing);
    var encKey = builder.Configuration["ENCKey"] ?? throw new InvalidOperationException(SystemMessages.EncKeyMissing);
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

var app = builder.Build();

app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        var exceptionFeature = context.Features.Get<IExceptionHandlerFeature>();
        var ex = exceptionFeature?.Error;

        if (ex is Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException
            or Microsoft.EntityFrameworkCore.DbUpdateException { InnerException: Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException })
        {
            var envelope = new ApiOperationResult<object?>().Failed(
                ApiMessages.ConcurrencyConflict,
                HttpStatusCode.Conflict,
                exMessage: app.Environment.IsDevelopment() ? ex.ToString() : null);

            context.Response.StatusCode = (int)envelope.Status;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(envelope);
            return;
        }

        var failure = new ApiOperationResult<object?>().Failed(
            ApiMessages.UnexpectedError,
            HttpStatusCode.InternalServerError,
            exMessage: app.Environment.IsDevelopment() ? ex?.Message : null);

        context.Response.StatusCode = (int)failure.Status;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsJsonAsync(failure);
    });
});

app.UseMiddleware<CorrelationIdMiddleware>();

// In development, we allow swagger even if not explicitly in Dev env for now to fix user issue
app.UseSwagger();
app.UseSwaggerUI(options =>
{
    var descriptions = app.DescribeApiVersions();
    foreach (var description in descriptions)
    {
        var url = $"/swagger/{description.GroupName}/swagger.json";
        var name = description.GroupName.ToUpperInvariant();
        options.SwaggerEndpoint(url, name);
    }
    options.RoutePrefix = "swagger"; // Explicitly set route prefix
});

app.UseCors("CorsPolicy");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health");

var logger = app.Services.GetRequiredService<ILogger<Program>>();
var addresses = app.Urls;
foreach (var address in addresses)
{
    logger.LogInformation("Application is running on: {Address}", address);
    logger.LogInformation("Swagger UI available at: {Address}/swagger", address);
}

app.Run();

namespace Core.API
{
    public sealed class SystemClock : IClock
    {
        public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
    }

    public sealed class HttpUserContext(IHttpContextAccessor accessor) : IUserContext
    {
        public string? UserId => accessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        public string? UserName => accessor.HttpContext?.User?.FindFirst(ClaimTypes.Name)?.Value;
        public IReadOnlyCollection<string> Roles => accessor.HttpContext?.User?.Claims.Where(x => x.Type == ClaimTypes.Role).Select(x => x.Value).Distinct().ToArray()
                                                    ?? Array.Empty<string>();
    }
}