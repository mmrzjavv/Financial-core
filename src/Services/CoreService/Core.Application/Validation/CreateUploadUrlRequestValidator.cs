using FluentValidation;
using Services.CoreService.Core.Application.Contracts.Documents;


namespace Services.CoreService.Core.Application.Validation;

public sealed class CreateUploadUrlRequestValidator : AbstractValidator<CreateUploadUrlRequest>
{
    public CreateUploadUrlRequestValidator()
    {
        RuleFor(x => x.DocumentType).IsInEnum();
        RuleFor(x => x.FileName).NotEmpty().MaximumLength(512);
        RuleFor(x => x.MimeType).NotEmpty().MaximumLength(128);
        RuleFor(x => x.FileSize).GreaterThan(0).LessThanOrEqualTo(100 * 1024 * 1024);
    }
}
