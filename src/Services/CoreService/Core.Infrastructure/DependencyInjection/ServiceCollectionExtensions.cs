using BuildingBlocks.Domain.Abstractions;
using BuildingBlocks.Application.Abstractions;
using Core.Application.Abstractions;
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

        services.AddScoped<IInvestmentCaseRepository, InvestmentCaseRepository>();
        services.AddScoped<BuildingBlocks.Persistence.Abstractions.IUnitOfWork, CoreUnitOfWork>();

        services.AddSingleton<ILiaraObjectStorage, LiaraObjectStorage>();
        services.AddScoped<IDocumentStorage, LiaraDocumentStorage>();


        return services;
    }
}
