using BuildingBlocks.Domain.Abstractions;
using BuildingBlocks.Application.Abstractions;
using Core.Application.Abstractions;
using Core.Application.Common;
using Core.Application.Abstractions.Persistence;
using Core.Application.Dashboard;
using Core.Application.Identity.Abstractions;
using Core.Application.Identity.Common.Options;
using Core.Application.Mappers;
using Core.Application.Notifications.Sms;
using Core.Infrastructure.Identity.Persistence;
using Core.Infrastructure.Notifications.Sms;
using Core.Infrastructure.Persistence;
using Core.Infrastructure.Storage;

using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using BuildingBlocks.Infrastructure.DependencyInjection;
using Core.Persistence.DependencyInjection;

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
        services.AddScoped<IGuaranteeCaseRepository, GuaranteeCaseRepository>();
        services.AddScoped<IGuaranteeRenewalCaseRepository, GuaranteeRenewalCaseRepository>();
        services.AddScoped<ILoanCaseRepository, LoanCaseRepository>();
        services.AddScoped<ICoreUnitOfWork, CoreUnitOfWork>();
        services.AddScoped<ICaseDtoMapper, CaseDtoMapper>();
        services.AddScoped<IGuaranteeCaseDtoMapper, GuaranteeCaseDtoMapper>();
        services.AddScoped<ILoanCaseDtoMapper, LoanCaseDtoMapper>();

        services.AddSingleton<ILiaraObjectStorage, LiaraObjectStorage>();
        services.AddScoped<IDocumentStorage, LiaraDocumentStorage>();

        services.Configure<SmsOptions>(configuration.GetSection("Sms"));
        services.Configure<LoanSettingsOptions>(configuration.GetSection(LoanSettingsOptions.SectionName));
        services.AddSingleton<SmsDispatchQueue>();
        services.AddScoped<ISmsDispatcher, SmsDispatcher>();
        services.AddScoped<ICaseWorkflowSmsNotifier, CaseWorkflowSmsNotifier>();

        var smsMongoEnabled = configuration.GetValue("Sms:MongoLogging:Enabled", false);
        if (smsMongoEnabled)
            services.AddSingleton<ISmsAuditStore, SmsMongoAuditStore>();
        else
            services.AddSingleton<ISmsAuditStore, NullSmsAuditStore>();

        if (configuration.GetValue("Sms:QueueEnabled", true))
            services.AddHostedService<SmsQueueBackgroundService>();

        services.AddHostedService<LoanInstallmentReminderBackgroundService>();

        services.AddScoped<IExecutiveDashboardService, ExecutiveDashboardService>();

        return services;
    }
}
