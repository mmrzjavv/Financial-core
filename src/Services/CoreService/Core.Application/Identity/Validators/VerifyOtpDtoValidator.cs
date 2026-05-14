using FluentValidation;
using Core.Application.Identity.DTOs.User;

namespace Core.Application.Identity.Validators;

public sealed class VerifyOtpDtoValidator : AbstractValidator<VerifyOtpDto>
{
    public VerifyOtpDtoValidator()
    {
        RuleFor(x => x.PhoneNumber)
            .NotEmpty().WithMessage("شماره موبایل الزامی است")
            .Matches(@"^09\d{9}$").WithMessage("فرمت شماره موبایل نامعتبر است");

        RuleFor(x => x.OtpCode)
            .NotEmpty().WithMessage("کد تایید الزامی است")
            .Length(6).WithMessage("کد تایید باید 6 رقم باشد")
            .Matches(@"^\d{6}$").WithMessage("کد تایید فقط باید شامل اعداد باشد");
    }
}
