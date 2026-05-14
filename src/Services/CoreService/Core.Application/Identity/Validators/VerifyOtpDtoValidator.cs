using Core.Application.Identity.Common;
using Core.Application.Identity.DTOs.User;
using FluentValidation;

namespace Core.Application.Identity.Validators;

public sealed class VerifyOtpDtoValidator : AbstractValidator<VerifyOtpDto>
{
    public VerifyOtpDtoValidator()
    {
        RuleFor(x => x.PhoneNumber)
            .NotEmpty().WithMessage(IdentityMessages.PhoneRequired)
            .Matches(@"^09\d{9}$").WithMessage(IdentityMessages.InvalidPhoneFormat);

        RuleFor(x => x.OtpCode)
            .NotEmpty().WithMessage(IdentityMessages.OtpCodeRequired)
            .Length(6).WithMessage(IdentityMessages.OtpCodeLength)
            .Matches(@"^\d{6}$").WithMessage(IdentityMessages.OtpCodeDigitsOnly);
    }
}
