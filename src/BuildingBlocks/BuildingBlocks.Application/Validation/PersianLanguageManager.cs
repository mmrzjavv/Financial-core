using System.Globalization;
using FluentValidation.Resources;

namespace BuildingBlocks.Application.Validation;

public sealed class PersianLanguageManager : LanguageManager
{
    public PersianLanguageManager()
    {
        Culture = new CultureInfo("fa-IR");

        AddTranslation("fa-IR", "NotEmptyValidator", "{PropertyName} الزامی است.");
        AddTranslation("fa-IR", "NotNullValidator", "{PropertyName} الزامی است.");
        AddTranslation("fa-IR", "MaximumLengthValidator", "{PropertyName} نباید بیشتر از {MaxLength} کاراکتر باشد.");
        AddTranslation("fa-IR", "MinimumLengthValidator", "{PropertyName} باید حداقل {MinLength} کاراکتر باشد.");
        AddTranslation("fa-IR", "LengthValidator", "{PropertyName} باید بین {MinLength} و {MaxLength} کاراکتر باشد.");
        AddTranslation("fa-IR", "ExactLengthValidator", "{PropertyName} باید {MaxLength} کاراکتر باشد.");
        AddTranslation("fa-IR", "GreaterThanOrEqualValidator", "{PropertyName} باید بزرگ‌تر یا مساوی {ComparisonValue} باشد.");
        AddTranslation("fa-IR", "GreaterThanValidator", "{PropertyName} باید بزرگ‌تر از {ComparisonValue} باشد.");
        AddTranslation("fa-IR", "LessThanOrEqualValidator", "{PropertyName} باید کوچک‌تر یا مساوی {ComparisonValue} باشد.");
        AddTranslation("fa-IR", "LessThanValidator", "{PropertyName} باید کوچک‌تر از {ComparisonValue} باشد.");
        AddTranslation("fa-IR", "EmailValidator", "فرمت ایمیل نامعتبر است.");
        AddTranslation("fa-IR", "RegularExpressionValidator", "فرمت {PropertyName} نامعتبر است.");
        AddTranslation("fa-IR", "EnumValidator", "{PropertyName} نامعتبر است.");
        AddTranslation("fa-IR", "IsInEnumValidator", "{PropertyName} نامعتبر است.");
        AddTranslation("fa-IR", "InclusiveBetweenValidator", "{PropertyName} باید بین {From} و {To} باشد.");
        AddTranslation("fa-IR", "ExclusiveBetweenValidator", "{PropertyName} باید بین {From} و {To} باشد.");
    }
}
