using FluentValidation;
using Core.Application.Identity.DTOs.User;

namespace Core.Application.Identity.Validators;

public sealed class SendOtpDtoValidator : AbstractValidator<SendOtpDto>
{
    public SendOtpDtoValidator()
    {
        RuleFor(x => x.PhoneNumber)
            .NotEmpty().WithMessage("شماره موبایل الزامی است")
            .Matches(@"^09\d{9}$").WithMessage("فرمت شماره موبایل نامعتبر است");
    }
}
