using Core.Application.Requests;
using FluentValidation;

namespace Core.Application.Validators;

public sealed class UpdateDataEntry2RequestValidator : AbstractValidator<UpdateDataEntry2Request>
{
    public UpdateDataEntry2RequestValidator()
    {
        RuleFor(x => x.InvestmentAttractionBasis).NotEmpty().MaximumLength(8000);
    }
}
