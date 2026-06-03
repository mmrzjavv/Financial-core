using System.Security.Claims;
using BuildingBlocks.Application.Validation;
using BuildingBlocks.Domain.Abstractions;
using BuildingBlocks.Observability.Logging;
using Core.API;
using Core.API.DependencyInjection;
using Core.Application.Abstractions;
using Core.Infrastructure.DependencyInjection;
using Core.Infrastructure.Identity.DependencyInjection;
using Core.Workflow.DependencyInjection;

ValidationLocalization.ConfigurePersian();

var builder = WebApplication.CreateBuilder(args);
builder.ConfigureHostUrls();
builder.Host.UsePlatformSerilog();

builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton<BuildingBlocks.Observability.Correlation.CorrelationIdMiddleware>();
builder.Services.AddSingleton<IClock, SystemClock>();
builder.Services.AddScoped<IUserContext, HttpUserContext>();

builder.Services.AddCoreApiServices();
builder.AddIdentityApplication();
builder.AddIdentityInfrastructure();
builder.Services.AddCoreInfrastructure(builder.Configuration);
builder.Services.AddCoreWorkflow(builder.Configuration, builder.Environment);
builder.Services.AddCoreApiPresentation(builder.Configuration);
builder.Services.AddCoreAuthentication(builder.Configuration);
builder.Services.AddCoreAuthorization();

var app = builder.Build();
app.UseCoreApiPipeline();

app.Run();
