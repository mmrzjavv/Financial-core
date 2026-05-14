// Modules/Panel/Validators/CreateUserDtoValidator.cs

using FluentValidation;
using Core.Application.Identity.DTOs.User;

namespace Core.Application.Identity.Validators
{
    public class CreateUserDtoValidator : AbstractValidator<CreateUserDto>
    {
        public CreateUserDtoValidator()
        {
            RuleFor(x => x.PhoneNumber)
                .NotEmpty()
                .Matches(@"^09\d{9}$").WithMessage("ÝÑãÊ ÔãÇÑå ãæÈÇíá ÕÍíÍ äíÓÊ");

            RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
            RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);

            RuleFor(x => x.Email)
                .NotEmpty()
                .EmailAddress().WithMessage("ÝÑãÊ Çíãíá ÕÍíÍ äíÓÊ")
                .MaximumLength(256);

            RuleFor(x => x.NationalCode)
                .Length(10)
                .Must(BeValidNationalCode).WithMessage("˜Ï ãáí äÇãÚÊÈÑ ÇÓÊ")
                .When(x => !string.IsNullOrWhiteSpace(x.NationalCode));
        }

        private bool BeValidNationalCode(string nationalCode)
        {
            if (string.IsNullOrEmpty(nationalCode) || nationalCode.Length != 10 || !nationalCode.All(char.IsDigit))
                return false;

            var check = int.Parse(nationalCode[9].ToString());
            var sum = 0;
            for (int i = 0; i < 9; i++)
                sum += int.Parse(nationalCode[i].ToString()) * (10 - i);

            var remainder = sum % 11;
            return (remainder < 2 && check == remainder) || (remainder >= 2 && check == 11 - remainder);
        }
    }
}
