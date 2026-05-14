using FluentValidation;
using Services.CoreService.Core.Application.Contracts.Documents;


namespace Services.CoreService.Core.Application.Validation;

public sealed class RegisterUploadedDocumentRequestValidator : AbstractValidator<RegisterUploadedDocumentRequest>
{
    public RegisterUploadedDocumentRequestValidator()
    {
        RuleFor(x => x.DocumentType).IsInEnum();
        RuleFor(x => x.S3Key).NotEmpty().MaximumLength(512);
        RuleFor(x => x.FileName).NotEmpty().MaximumLength(512);
        RuleFor(x => x.MimeType).NotEmpty().MaximumLength(128);
        RuleFor(x => x.FileSize).GreaterThan(0);
        RuleFor(x => x.Version).GreaterThan(0);
    }
}
