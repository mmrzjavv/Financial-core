using Core.Application.Identity.Common;
using Core.Application.Identity.DTOs.User;
using FluentValidation;

namespace Core.Application.Identity.Validators;

public sealed class UpdateUserDtoValidator : AbstractValidator<UpdateUserDto>
{
    public UpdateUserDtoValidator()
    {
        RuleFor(x => x.PhoneNumber)
            .Matches(@"^09\d{9}$").WithMessage(IdentityMessages.InvalidPhoneFormat)
            .When(x => !string.IsNullOrWhiteSpace(x.PhoneNumber));

        RuleFor(x => x.Email)
            .EmailAddress().WithMessage(IdentityMessages.InvalidEmailFormat)
            .MaximumLength(256)
            .When(x => !string.IsNullOrWhiteSpace(x.Email));

        RuleFor(x => x.FirstName)
            .MaximumLength(100).WithMessage(IdentityMessages.FirstNameMaxLength)
            .When(x => !string.IsNullOrEmpty(x.FirstName));

        RuleFor(x => x.LastName)
            .MaximumLength(100).WithMessage(IdentityMessages.LastNameMaxLength)
            .When(x => !string.IsNullOrEmpty(x.LastName));

        RuleFor(x => x.NationalCode)
            .Length(10).WithMessage(IdentityMessages.NationalCodeLength)
            .Matches(@"^\d{10}$").WithMessage(IdentityMessages.NationalCodeDigitsOnly)
            .When(x => !string.IsNullOrEmpty(x.NationalCode));
    }
}
