using Core.Application.Identity.Common;
using Core.Application.Identity.DTOs.User;
using FluentValidation;

namespace Core.Application.Identity.Validators;

public sealed class SendOtpDtoValidator : AbstractValidator<SendOtpDto>
{
    public SendOtpDtoValidator()
    {
        RuleFor(x => x.PhoneNumber)
            .NotEmpty().WithMessage(IdentityMessages.PhoneRequired)
            .Matches(@"^09\d{9}$").WithMessage(IdentityMessages.InvalidPhoneFormat);
    }
}
