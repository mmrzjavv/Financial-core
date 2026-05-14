using FluentValidation;
using Services.CoreService.Core.Application.Contracts.Comments;


namespace Services.CoreService.Core.Application.Validation;

public sealed class AddCommentRequestValidator : AbstractValidator<AddCommentRequest>
{
    public AddCommentRequestValidator()
    {
        RuleFor(x => x.Phase).IsInEnum();
        RuleFor(x => x.Message).NotEmpty().MaximumLength(8000);
    }
}
