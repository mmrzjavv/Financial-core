using FluentValidation;
using Services.CoreService.Core.Application.Contracts.DataEntry;

namespace Services.CoreService.Core.Application.Validation;

public sealed class DataEntry2UpsertRequestValidator : AbstractValidator<DataEntry2UpsertRequest>
{
    public DataEntry2UpsertRequestValidator()
    {
        RuleFor(x => x.InvestmentAttractionBasis).NotEmpty().MaximumLength(8000);
    }
}
