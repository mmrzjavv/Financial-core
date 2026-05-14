using FluentValidation;
using Core.Application.Identity.DTOs.User;

namespace Core.Application.Identity.Validators
{
    public class UpdateUserDtoValidator : AbstractValidator<UpdateUserDto>
    {
        public UpdateUserDtoValidator()
        {
            RuleFor(x => x.PhoneNumber)
                .Matches(@"^09\d{9}$").WithMessage("ÝÑãÊ ÔãÇÑå ãæÈÇíá ÕÍíÍ äíÓÊ")
                .When(x => !string.IsNullOrWhiteSpace(x.PhoneNumber));

            RuleFor(x => x.Email)
                .EmailAddress().WithMessage("ÝÑãÊ Çíãíá ÕÍíÍ äíÓÊ")
                .MaximumLength(256)
                .When(x => !string.IsNullOrWhiteSpace(x.Email));

            RuleFor(x => x.FirstName)
                .MaximumLength(100).WithMessage("äÇã äÈÇíÏ ÈíÔÊÑ ÇÒ 100 ˜ÇÑÇ˜ÊÑ ÈÇÔÏ")
                .When(x => !string.IsNullOrEmpty(x.FirstName));

            RuleFor(x => x.LastName)
                .MaximumLength(100).WithMessage("äÇã ÎÇäæÇÏí äÈÇíÏ ÈíÔÊÑ ÇÒ 100 ˜ÇÑÇ˜ÊÑ ÈÇÔÏ")
                .When(x => !string.IsNullOrEmpty(x.LastName));

            RuleFor(x => x.NationalCode)
                .Length(10).WithMessage("˜Ï ãáí ÈÇíÏ 10 ÑÞã ÈÇÔÏ")
                .Matches(@"^\d{10}$").WithMessage("˜Ï ãáí ÝÞØ ÈÇíÏ ÔÇãá ÇÚÏÇÏ ÈÇÔÏ")
                .When(x => !string.IsNullOrEmpty(x.NationalCode));
        }
    }
}
