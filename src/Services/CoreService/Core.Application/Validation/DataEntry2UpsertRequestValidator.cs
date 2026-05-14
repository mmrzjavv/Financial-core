using FluentValidation;
using Services.CoreService.Core.Application.Contracts.DataEntry;


namespace Services.CoreService.Core.Application.Validation;

public sealed class DataEntry2UpsertRequestValidator : AbstractValidator<DataEntry2UpsertRequest>
{
    public DataEntry2UpsertRequestValidator()
    {
        RuleFor(x => x.MarketAnalysis).NotEmpty().MaximumLength(8000);
        RuleFor(x => x.RevenueModel).NotEmpty().MaximumLength(8000);
        RuleFor(x => x.CompetitiveAdvantage).NotEmpty().MaximumLength(8000);
        RuleFor(x => x.FinancialProjection).NotEmpty().MaximumLength(8000);
        RuleFor(x => x.Risks).MaximumLength(8000);
        RuleFor(x => x.GoToMarketStrategy).MaximumLength(8000);
    }
}
