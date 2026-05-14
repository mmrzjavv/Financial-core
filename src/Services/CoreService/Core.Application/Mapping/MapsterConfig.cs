using Mapster;
using Services.CoreService.Core.Application.Contracts.Cases;
using Services.CoreService.Core.Domain.Entities;


namespace Services.CoreService.Core.Application.Mapping;

public sealed class MapsterConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<InvestmentCase, CaseDto>();
    }
}
