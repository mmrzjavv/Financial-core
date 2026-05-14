using FluentValidation;
using Core.Application.Requests;


namespace Core.Application.Validators;

public sealed class PresignUploadRequestValidator : AbstractValidator<PresignUploadRequest>
{
    public PresignUploadRequestValidator()
    {
        RuleFor(x => x.DocumentType).IsInEnum();
        RuleFor(x => x.FileName).NotEmpty().MaximumLength(512);
        RuleFor(x => x.MimeType).NotEmpty().MaximumLength(256);
        RuleFor(x => x.FileSize).GreaterThan(0).LessThanOrEqualTo(250L * 1024 * 1024);
    }
}
