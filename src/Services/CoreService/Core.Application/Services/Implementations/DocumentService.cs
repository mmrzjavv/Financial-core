using BuildingBlocks.Application.Validation;
using Core.Application.Common;
using System.Text.RegularExpressions;
using BuildingBlocks.Application.Abstractions;
using BuildingBlocks.Application.Errors;
using BuildingBlocks.Application.Results;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Services.CoreService.Core.Application.Abstractions;
using Services.CoreService.Core.Application.Contracts.Documents;
using Services.CoreService.Core.Domain.Constants;
using Services.CoreService.Core.Domain.Enums;



namespace Services.CoreService.Core.Application.Services.Implementations;

public sealed class DocumentService : IDocumentService
{
    private static readonly Regex Unsafe = new(@"[^a-zA-Z0-9\.\-_]+", RegexOptions.Compiled);

    private readonly ICoreDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IFileStorage _fileStorage;
    private readonly IValidator<CreateUploadUrlRequest> _createValidator;
    private readonly IValidator<RegisterUploadedDocumentRequest> _registerValidator;

    public DocumentService(
        ICoreDbContext db,
        ICurrentUser currentUser,
        IFileStorage fileStorage,
        IValidator<CreateUploadUrlRequest> createValidator,
        IValidator<RegisterUploadedDocumentRequest> registerValidator)
    {
        _db = db;
        _currentUser = currentUser;
        _fileStorage = fileStorage;
        _createValidator = createValidator;
        _registerValidator = registerValidator;
    }

    public async Task<Result<PresignedUrlResponse>> CreatePresignedUploadUrlAsync(Guid caseId, CreateUploadUrlRequest request, CancellationToken ct)
    {
        var validation = await _createValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
            return Result<PresignedUrlResponse>.Fail(Error.Validation(validation.ToErrorMessage()));

        var entity = await _db.InvestmentCases.AsNoTracking().FirstOrDefaultAsync(x => x.Id == caseId, ct);
        if (entity is null)
            return Result<PresignedUrlResponse>.Fail(Error.NotFound(ApiMessages.CaseNotFound));

        if (entity.ApplicantUserId != _currentUser.UserId && !_currentUser.Roles.Contains(SystemRoles.Admin))
            return Result<PresignedUrlResponse>.Fail(Error.Forbidden());

        var nextVersion = await _db.CaseDocuments
            .AsNoTracking()
            .Where(x => x.CaseId == caseId && x.DocumentType == request.DocumentType)
            .Select(x => (int?)x.Version)
            .MaxAsync(ct) ?? 0;
        nextVersion++;

        var safeFileName = Unsafe.Replace(request.FileName, "_");
        var key = $"cases/{caseId:D}/{request.DocumentType}/{nextVersion:D4}/{safeFileName}";

        var url = await _fileStorage.CreatePresignedUploadUrlAsync(key, request.MimeType, TimeSpan.FromMinutes(15), ct);
        return Result<PresignedUrlResponse>.Ok(new PresignedUrlResponse(url, key, nextVersion));
    }

    public async Task<Result> RegisterUploadedDocumentAsync(Guid caseId, RegisterUploadedDocumentRequest request, CancellationToken ct)
    {
        var validation = await _registerValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
            return Result.Fail(Error.Validation(validation.ToErrorMessage()));

        var entity = await _db.InvestmentCases
            .Include(x => x.Documents)
            .FirstOrDefaultAsync(x => x.Id == caseId, ct);

        if (entity is null)
            return Result.Fail(Error.NotFound(ApiMessages.CaseNotFound));

        if (entity.ApplicantUserId != _currentUser.UserId && !_currentUser.Roles.Contains(SystemRoles.Admin))
            return Result.Fail(Error.Forbidden());

        if (entity.Documents.Any(d => d.S3Key == request.S3Key))
            return Result.Fail(Error.Conflict(ApiMessages.DocumentAlreadyRegistered));

        entity.AddDocument(
            s3Key: request.S3Key,
            fileName: request.FileName,
            mimeType: request.MimeType,
            fileSize: request.FileSize,
            version: request.Version,
            documentType: request.DocumentType,
            uploadedByUserId: _currentUser.UserId);

        await _db.SaveChangesAsync(ct);
        return Result.Ok();
    }
}