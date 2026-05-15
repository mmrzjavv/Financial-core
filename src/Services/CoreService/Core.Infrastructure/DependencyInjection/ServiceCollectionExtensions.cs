using BuildingBlocks.Domain.Abstractions;
using BuildingBlocks.Application.Abstractions;
using Core.Application.Abstractions;
using Core.Application.Abstractions.Persistence;
using Core.Application.Identity.Abstractions;
using Core.Application.Mappers;
using Core.Infrastructure.Identity.Persistence;
using Core.Infrastructure.Persistence;
using Core.Infrastructure.Storage;

using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using BuildingBlocks.Infrastructure.DependencyInjection;
using Services.CoreService.Core.Persistence;
using Services.CoreService.Core.Persistence.DependencyInjection;

namespace Core.Infrastructure.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCoreInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<InvestmentCaseRepository>());

        services.AddBuildingBlocksInfrastructure(configuration);
        services.AddCorePersistence(configuration);

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<IUserSessionRepository, UserSessionRepository>();
        services.AddScoped<ICompanyRepository, CompanyRepository>();
        services.AddScoped<IInvestmentCaseRepository, InvestmentCaseRepository>();
        services.AddScoped<ICoreUnitOfWork, CoreUnitOfWork>();
        services.AddScoped<ICaseDtoMapper, CaseDtoMapper>();

        services.AddSingleton<ILiaraObjectStorage, LiaraObjectStorage>();
        services.AddScoped<IDocumentStorage, LiaraDocumentStorage>();


        return services;
    }
}
