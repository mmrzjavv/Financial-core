using FluentValidation;
using Core.Application.Requests;


namespace Core.Application.Validators;

public sealed class UpdateDataEntry2RequestValidator : AbstractValidator<UpdateDataEntry2Request>
{
    public UpdateDataEntry2RequestValidator()
    {
        RuleFor(x => x.MarketAnalysis).NotEmpty().MaximumLength(4000);
        RuleFor(x => x.RevenueModel).NotEmpty().MaximumLength(2000);
        RuleFor(x => x.CompetitiveAdvantage).NotEmpty().MaximumLength(4000);
        RuleFor(x => x.FinancialProjection).MaximumLength(4000);
    }
}
