using BuildingBlocks.Application.Errors;
using BuildingBlocks.Application.Results;
using BuildingBlocks.Application.Abstractions;
using BuildingBlocks.Domain.Abstractions;
using BuildingBlocks.Observability.Correlation;
using BuildingBlocks.Persistence.Abstractions;
using Core.Application.Authorization;
using Core.Application.Abstractions;
using Core.Application.DTOs;
using Core.Application.Requests;
using Core.Application.Responses;
using Microsoft.AspNetCore.Http;
using Services.CoreService.Core.Domain.Constants;
using Services.CoreService.Core.Domain.Entities;
using Services.CoreService.Core.Domain.Enums;

namespace Core.Application.Services;

public sealed class InvestmentCaseAppService(
    IInvestmentCaseRepository repository,
    IUnitOfWork unitOfWork,
    ICaseStateManager stateManager,
    ICaseWorkflowOrchestrator workflowOrchestrator,
    ICaseNumberGenerator caseNumberGenerator,
    IDocumentStorage documentStorage,
    BuildingBlocks.Domain.Abstractions.IClock clock,
    IUserContext userContext,
    ICaseAuthorizationService authorizationService,
    IHttpContextAccessor httpContextAccessor) : IInvestmentCaseAppService
{
    public async Task<Result<InvestmentCaseDto>> CreateAsync(CreateInvestmentCaseRequest request, CancellationToken cancellationToken)
    {
        var authResult = RequireUserId();
        if (authResult.IsFailure) return Result<InvestmentCaseDto>.Fail(authResult.Error!);

        if (!authorizationService.HasPermission(CasePermissions.Create) || authorizationService.IsInternalUser)
            return Result<InvestmentCaseDto>.Fail(Error.Forbidden("Not allowed."));

        var now = clock.UtcNow;
        var caseNumber = await caseNumberGenerator.GenerateAsync(cancellationToken);
        var entity = new InvestmentCase(caseNumber, authResult.Value!, request.ApplicantType);
        if (request.ApplicantType == ApplicantType.Company)
        {
            if (request.Company is null)
                return Result<InvestmentCaseDto>.Fail(Error.Validation("Company is required for ApplicantType=Company."));

            entity.UpsertCompanyProfile(
                request.Company.Name,
                request.Company.EconomicCode,
                request.Company.RegistrationNumber,
                request.Company.NationalId,
                request.Company.PhoneNumber,
                request.Company.Address,
                request.Company.City,
                request.Company.Province,
                request.Company.PostalCode);
        }
        var workflowInstanceId = await workflowOrchestrator.StartAsync(entity.Id, cancellationToken);
        entity.AttachWorkflowInstance(workflowInstanceId);

        await repository.AddAsync(entity, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<InvestmentCaseDto>.Ok(ToApplicantDto(entity, now));
    }

    public async Task<Result<InvestmentCaseDto>> GetAsync(Guid caseId, CancellationToken cancellationToken)
    {
        var authResult = RequireUserId();
        if (authResult.IsFailure) return Result<InvestmentCaseDto>.Fail(authResult.Error!);

        var entity = await repository.GetScopedAsync(caseId, authResult.Value!, authorizationService.IsInternalUser, cancellationToken);
        if (entity is null)
            return Result<InvestmentCaseDto>.Fail(Error.NotFound("Investment case not found."));

        return Result<InvestmentCaseDto>.Ok(ToDto(entity, clock.UtcNow, authorizationService.IsInternalUser));
    }

    public async Task<Result> UpdateDataEntry1Async(Guid caseId, UpdateDataEntry1Request request, CancellationToken cancellationToken)
    {
        var authResult = RequireUserId();
        if (authResult.IsFailure) return Result.Fail(authResult.Error!);

        if (!userContext.Roles.Contains(SystemRoles.Applicant))
            return Result.Fail(Error.Forbidden("Not allowed."));

        var entity = await repository.GetScopedAsync(caseId, authResult.Value!, isInternalUser: false, cancellationToken);
        if (entity is null)
            return Result.Fail(Error.NotFound("Investment case not found."));

        if (entity.CurrentStatus is not (CaseStatus.Draft or CaseStatus.DataEntry1))
            return Result.Fail(Error.Conflict("Data Entry 1 cannot be edited in the current status."));

        entity.UpsertDataEntry1(
            request.StartupTitle,
            request.BusinessDescription,
            request.RequestedAmount,
            request.TeamSize,
            request.Website,
            request.Country,
            request.City,
            industry: null);

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Ok();
    }

    public async Task<Result> UpdateDataEntry2Async(Guid caseId, UpdateDataEntry2Request request, CancellationToken cancellationToken)
    {
        var authResult = RequireUserId();
        if (authResult.IsFailure) return Result.Fail(authResult.Error!);

        if (!userContext.Roles.Contains(SystemRoles.Applicant))
            return Result.Fail(Error.Forbidden("Not allowed."));

        var entity = await repository.GetScopedAsync(caseId, authResult.Value!, isInternalUser: false, cancellationToken);
        if (entity is null)
            return Result.Fail(Error.NotFound("Investment case not found."));

        if (entity.CurrentStatus is not CaseStatus.DataEntry2)
            return Result.Fail(Error.Conflict("Data Entry 2 cannot be edited in the current status."));

        entity.UpsertDataEntry2(
            request.MarketAnalysis,
            request.RevenueModel,
            request.CompetitiveAdvantage,
            request.FinancialProjection ?? string.Empty,
            risks: null,
            goToMarketStrategy: null);

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Ok();
    }

    public async Task<Result> UpdateFinancialWorksheetAsync(Guid caseId, UpdateFinancialWorksheetRequest request, CancellationToken cancellationToken)
    {
        var authResult = RequireUserId();
        if (authResult.IsFailure) return Result.Fail(authResult.Error!);

        if (!authorizationService.HasPermission(CasePermissions.ManageFinancialWorksheet))
            return Result.Fail(Error.Forbidden("Not allowed."));

        var entity = await repository.GetScopedAsync(caseId, authResult.Value!, authorizationService.IsInternalUser, cancellationToken);
        if (entity is null)
            return Result.Fail(Error.NotFound("Investment case not found."));

        if (entity.CurrentStatus is not CaseStatus.WaitingFinancialWorksheet)
            return Result.Fail(Error.Conflict("Financial worksheet cannot be edited in the current status."));

        entity.UpsertFinancialWorksheet(
            request.BankName,
            request.Iban,
            request.ApprovedAmount ?? 0m,
            request.PaymentSchedule ?? string.Empty,
            request.Notes);

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Ok();
    }

    public async Task<Result> SubmitDataEntry1Async(Guid caseId, string? comment, CancellationToken ct)
        => await ApplyTransitionAsync(caseId, WorkflowAction.Submit, comment, ct);

    public async Task<Result> ApproveDataEntry1Async(Guid caseId, string? comment, CancellationToken ct)
        => await ApplyTransitionAsync(caseId, WorkflowAction.Approve, comment, ct);

    public async Task<Result> RequestDataEntry1RevisionAsync(Guid caseId, string message, CancellationToken ct)
        => await ApplyTransitionAsync(caseId, WorkflowAction.RequestRevision, message, ct);

    public async Task<Result> SubmitDataEntry2Async(Guid caseId, string? comment, CancellationToken ct)
        => await ApplyTransitionAsync(caseId, WorkflowAction.Submit, comment, ct);

    public async Task<Result> ApproveDataEntry2Async(Guid caseId, string? comment, CancellationToken ct)
        => await ApplyTransitionAsync(caseId, WorkflowAction.Approve, comment, ct);

    public async Task<Result> RequestDataEntry2RevisionAsync(Guid caseId, string message, CancellationToken ct)
        => await ApplyTransitionAsync(caseId, WorkflowAction.RequestRevision, message, ct);

    public async Task<Result> ApproveInitialValuationAsync(Guid caseId, string? comment, CancellationToken ct)
    {
        if (!authorizationService.HasPermission(CasePermissions.ManageValuations))
            return Result.Fail(Error.Forbidden("Not allowed."));

        return await ApplyTransitionAsync(caseId, WorkflowAction.Approve, comment, ct);
    }

    public async Task<Result> ApproveSecondaryValuationAsync(Guid caseId, string? comment, CancellationToken ct)
    {
        if (!authorizationService.HasPermission(CasePermissions.ManageValuations))
            return Result.Fail(Error.Forbidden("Not allowed."));

        return await ApplyTransitionAsync(caseId, WorkflowAction.Approve, comment, ct);
    }

    public async Task<Result> UploadPreliminaryContractAsync(Guid caseId, string s3Key, CancellationToken ct)
    {
        if (!authorizationService.HasPermission(CasePermissions.ManageContracts))
            return Result.Fail(Error.Forbidden("Not allowed."));

        var result = await ConfirmDocumentUploadedAsync(caseId, s3Key, ct);
        if (result.IsFailure) return Result.Fail(result.Error!);
        return await ApplyTransitionAsync(caseId, WorkflowAction.UploadPreliminaryContract, "Preliminary contract uploaded", ct);
    }

    public async Task<Result> ApprovePreliminaryContractAsync(Guid caseId, string? comment, CancellationToken ct)
        => await ApplyTransitionAsync(caseId, WorkflowAction.Approve, comment, ct);

    public async Task<Result> RequestPreliminaryContractRevisionAsync(Guid caseId, string message, CancellationToken ct)
        => await ApplyTransitionAsync(caseId, WorkflowAction.RequestRevision, message, ct);

    public async Task<Result> FinalizeContractDraftAsync(Guid caseId, string? comment, CancellationToken ct)
    {
        if (!authorizationService.HasPermission(CasePermissions.ManageContracts))
            return Result.Fail(Error.Forbidden("Not allowed."));

        return await ApplyTransitionAsync(caseId, WorkflowAction.FinalizeContractDraft, comment, ct);
    }

    public async Task<Result> ConfirmSignatureAsync(Guid caseId, string? comment, CancellationToken ct)
    {
        if (!authorizationService.HasPermission(CasePermissions.ManageContracts))
            return Result.Fail(Error.Forbidden("Not allowed."));

        return await ApplyTransitionAsync(caseId, WorkflowAction.ConfirmSignature, comment, ct);
    }

    public async Task<Result> UploadSignedContractAsync(Guid caseId, string s3Key, CancellationToken ct)
    {
        if (!authorizationService.HasPermission(CasePermissions.ManageContracts))
            return Result.Fail(Error.Forbidden("Not allowed."));

        var result = await ConfirmDocumentUploadedAsync(caseId, s3Key, ct);
        if (result.IsFailure) return Result.Fail(result.Error!);
        return await ApplyTransitionAsync(caseId, WorkflowAction.UploadSignedContract, "Signed contract uploaded", ct);
    }

    public async Task<Result> SubmitFinancialWorksheetAsync(Guid caseId, string? comment, CancellationToken ct)
    {
        if (!authorizationService.HasPermission(CasePermissions.ManageFinancialWorksheet))
            return Result.Fail(Error.Forbidden("Not allowed."));

        return await ApplyTransitionAsync(caseId, WorkflowAction.SubmitFinancialWorksheet, comment, ct);
    }

    public async Task<Result> ApproveFinancialWorksheetAsync(Guid caseId, string? comment, CancellationToken ct)
    {
        if (!authorizationService.HasPermission(CasePermissions.ManageFinancialWorksheet))
            return Result.Fail(Error.Forbidden("Not allowed."));

        return await ApplyTransitionAsync(caseId, WorkflowAction.ApproveFinancialWorksheet, comment, ct);
    }

    public async Task<Result> RequestFinancialWorksheetRevisionAsync(Guid caseId, string message, CancellationToken ct)
    {
        if (!authorizationService.HasPermission(CasePermissions.ManageFinancialWorksheet))
            return Result.Fail(Error.Forbidden("Not allowed."));

        return await ApplyTransitionAsync(caseId, WorkflowAction.RequestRevision, message, ct);
    }

    public async Task<Result> ConfirmPaymentAsync(Guid caseId, Guid paymentId, CancellationToken ct)
    {
        var authResult = RequireUserId();
        if (authResult.IsFailure) return Result.Fail(authResult.Error!);

        if (!authorizationService.HasPermission(CasePermissions.ManagePayments))
            return Result.Fail(Error.Forbidden("Not allowed."));

        var entity = await repository.GetScopedAsync(caseId, authResult.Value!, isInternalUser: true, ct);
        if (entity is null) return Result.Fail(Error.NotFound("Case not found"));

        var payment = entity.Payments.FirstOrDefault(x => x.Id == paymentId);
        if (payment is null) return Result.Fail(Error.NotFound("Payment not found"));

        payment.Update(payment.Amount, payment.PaymentDate, payment.TransactionNumber, payment.ReceiptS3Key, payment.Notes, payment.Method, PaymentStatus.Completed);

        entity.CheckPaymentCompletion(authResult.Value!);
        await unitOfWork.SaveChangesAsync(ct);
        return Result.Ok();
    }

    public async Task<Result> CancelPaymentAsync(Guid caseId, Guid paymentId, CancellationToken ct)
    {
        var authResult = RequireUserId();
        if (authResult.IsFailure) return Result.Fail(authResult.Error!);

        if (!authorizationService.HasPermission(CasePermissions.ManagePayments))
            return Result.Fail(Error.Forbidden("Not allowed."));

        var entity = await repository.GetScopedAsync(caseId, authResult.Value!, isInternalUser: true, ct);
        if (entity is null) return Result.Fail(Error.NotFound("Case not found"));

        var payment = entity.Payments.FirstOrDefault(x => x.Id == paymentId);
        if (payment is null) return Result.Fail(Error.NotFound("Payment not found"));

        payment.Update(payment.Amount, payment.PaymentDate, payment.TransactionNumber, payment.ReceiptS3Key, payment.Notes, payment.Method, PaymentStatus.Cancelled);
        await unitOfWork.SaveChangesAsync(ct);
        return Result.Ok();
    }

    public async Task<Result> RejectAsync(Guid caseId, string reason, CancellationToken ct)
    {
        return await ApplyTransitionAsync(caseId, WorkflowAction.Reject, $"Rejected: {reason}", ct);
    }

    public async Task<Result> CancelAsync(Guid caseId, string reason, CancellationToken ct)
        => await ApplyTransitionAsync(caseId, WorkflowAction.Cancel, $"Cancelled: {reason}", ct);

    public async Task<Result> ArchiveAsync(Guid caseId, string reason, CancellationToken ct)
        => await ApplyTransitionAsync(caseId, WorkflowAction.Archive, $"Archived: {reason}", ct);

    private async Task<Result> ApplyTransitionAsync(Guid caseId, WorkflowAction action, string? comment, CancellationToken ct)
    {
        var authResult = RequireUserId();
        if (authResult.IsFailure) return Result.Fail(authResult.Error!);

        var entity = await repository.GetScopedAsync(caseId, authResult.Value!, authorizationService.IsInternalUser, ct);
        if (entity is null) return Result.Fail(Error.NotFound("Case not found"));

        var actorRole = ResolveActorRole();

        var correlationId = ResolveCorrelationGuid(httpContextAccessor.HttpContext);
        var transition = await stateManager.TransitionAsync(entity, action, authResult.Value!, actorRole, comment, correlationId);
        if (transition.IsFailure) return transition;

        await workflowOrchestrator.SignalAsync(
            caseId,
            WorkflowSignals.StatusChanged,
            new { status = entity.CurrentStatus, action = action.ToString() },
            ct);

        await unitOfWork.SaveChangesAsync(ct);
        return Result.Ok();
    }

    private static Guid ResolveCorrelationGuid(HttpContext? httpContext)
    {
        var raw = httpContext?.Items[CorrelationContext.ItemKey]?.ToString()
                  ?? httpContext?.Request.Headers[CorrelationContext.HeaderName].ToString()
                  ?? httpContext?.TraceIdentifier;

        if (string.IsNullOrWhiteSpace(raw))
            return Guid.NewGuid();

        if (Guid.TryParse(raw, out var parsed))
            return parsed;

        using var sha = System.Security.Cryptography.SHA256.Create();
        var bytes = sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(raw));
        var guidBytes = bytes.Take(16).ToArray();
        return new Guid(guidBytes);
    }

    private string ResolveActorRole()
    {
        if (userContext.Roles.Contains(SystemRoles.Admin)) return SystemRoles.Admin;
        if (userContext.Roles.Contains(SystemRoles.InvestmentManager)) return SystemRoles.InvestmentManager;
        if (userContext.Roles.Contains(SystemRoles.InvestmentExpert)) return SystemRoles.InvestmentExpert;
        if (userContext.Roles.Contains(SystemRoles.LegalExpert)) return SystemRoles.LegalExpert;
        if (userContext.Roles.Contains(SystemRoles.FinancialExpert)) return SystemRoles.FinancialExpert;
        if (userContext.Roles.Contains(SystemRoles.Applicant)) return SystemRoles.Applicant;
        return userContext.Roles.FirstOrDefault() ?? string.Empty;
    }

    public async Task<Result> RecordValuationAsync(Guid caseId, RecordValuationRequest request, CancellationToken cancellationToken)
    {
        var authResult = RequireUserId();
        if (authResult.IsFailure) return Result.Fail(authResult.Error!);

        if (!authorizationService.HasPermission(CasePermissions.ManageValuations))
            return Result.Fail(Error.Forbidden("Not allowed."));

        var entity = await repository.GetScopedAsync(caseId, authResult.Value!, isInternalUser: true, cancellationToken);
        if (entity is null)
            return Result.Fail(Error.NotFound("Investment case not found."));

        var requiredStatus = request.Type == ValuationType.Primary ? CaseStatus.InitialValuation : CaseStatus.SecondaryValuation;
        if (entity.CurrentStatus != requiredStatus)
            return Result.Fail(Error.Conflict($"Valuation can only be recorded during {requiredStatus} status."));

        entity.AddValuation(request.Type, request.Amount, request.Notes, authResult.Value!);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Ok();
    }

    public async Task<Result> RecordPaymentAsync(Guid caseId, RecordPaymentRequest request, CancellationToken cancellationToken)
    {
        var authResult = RequireUserId();
        if (authResult.IsFailure) return Result.Fail(authResult.Error!);

        if (!authorizationService.HasPermission(CasePermissions.ManagePayments))
            return Result.Fail(Error.Forbidden("Not allowed."));

        var entity = await repository.GetScopedAsync(caseId, authResult.Value!, isInternalUser: true, cancellationToken);
        if (entity is null)
            return Result.Fail(Error.NotFound("Investment case not found."));

        if (entity.CurrentStatus != CaseStatus.WaitingPayment)
            return Result.Fail(Error.Conflict("Payments can only be recorded during WaitingPayment status."));

        entity.AddPayment(
            request.Amount,
            request.PaymentDate,
            request.TransactionNumber,
            request.ReceiptS3Key,
            request.Notes,
            request.Method,
            request.Status,
            authResult.Value!);

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Ok();
    }

    public async Task<Result> UpdatePaymentAsync(Guid caseId, Guid paymentId, UpdatePaymentRequest request, CancellationToken cancellationToken)
    {
        var authResult = RequireUserId();
        if (authResult.IsFailure) return Result.Fail(authResult.Error!);

        if (!authorizationService.HasPermission(CasePermissions.ManagePayments))
            return Result.Fail(Error.Forbidden("Not allowed."));

        var entity = await repository.GetScopedAsync(caseId, authResult.Value!, isInternalUser: true, cancellationToken);
        if (entity is null)
            return Result.Fail(Error.NotFound("Investment case not found."));

        var payment = entity.Payments.FirstOrDefault(x => x.Id == paymentId);
        if (payment is null)
            return Result.Fail(Error.NotFound("Payment record not found."));

        payment.Update(
            request.Amount,
            request.PaymentDate,
            request.TransactionNumber,
            request.ReceiptS3Key,
            request.Notes,
            request.Method,
            request.Status);

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Ok();
    }

    public async Task<Result> DeletePaymentAsync(Guid caseId, Guid paymentId, CancellationToken cancellationToken)
    {
        var authResult = RequireUserId();
        if (authResult.IsFailure) return Result.Fail(authResult.Error!);

        if (!authorizationService.HasPermission(CasePermissions.ManagePayments))
            return Result.Fail(Error.Forbidden("Not allowed."));

        var entity = await repository.GetScopedAsync(caseId, authResult.Value!, isInternalUser: true, cancellationToken);
        if (entity is null)
            return Result.Fail(Error.NotFound("Investment case not found."));

        var payment = entity.Payments.FirstOrDefault(x => x.Id == paymentId);
        if (payment is null)
            return Result.Fail(Error.NotFound("Payment record not found."));

        entity.Payments.Remove(payment);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Ok();
    }

    public async Task<Result<IEnumerable<CaseCommentDto>>> GetCommentsAsync(Guid caseId, bool includeInternal, CancellationToken cancellationToken)
    {
        var authResult = RequireUserId();
        if (authResult.IsFailure) return Result<IEnumerable<CaseCommentDto>>.Fail(authResult.Error!);

        var entity = await repository.GetScopedAsync(caseId, authResult.Value!, authorizationService.IsInternalUser, cancellationToken);
        if (entity is null)
            return Result<IEnumerable<CaseCommentDto>>.Fail(Error.NotFound("Investment case not found."));

        var canViewInternal = includeInternal && authorizationService.HasPermission(CasePermissions.ViewInternalComments);
        var isInternalView = authorizationService.IsInternalUser;

        var comments = entity.Comments
            .Where(x => (isInternalView && canViewInternal) || !x.IsInternal)
            .Select(x =>
                isInternalView
                    ? (CaseCommentDto)new CaseCommentInternalDto(
                        x.Id,
                        x.CaseId,
                        x.Phase,
                        x.SenderUserId,
                        x.SenderRole,
                        x.Message,
                        x.IsRevisionRequest,
                        x.IsInternal,
                        x.ParentId,
                        x.Attachments.Select(a => new CaseCommentAttachmentInternalDto(a.Id, a.S3Key, a.FileName)),
                        x.CreatedAt)
                    : (CaseCommentDto)new CaseCommentApplicantDto(
                        x.Id,
                        x.CaseId,
                        x.Phase,
                        x.Message,
                        x.IsRevisionRequest,
                        x.ParentId,
                        x.Attachments.Select(a => new CaseCommentAttachmentApplicantDto(a.Id, a.FileName)),
                        x.CreatedAt))
            .OrderBy(x => x.CreatedAt);

        return Result<IEnumerable<CaseCommentDto>>.Ok(comments);
    }

    public async Task<Result> AddCommentAsync(Guid caseId, AddCommentRequest request, CancellationToken cancellationToken)
    {
        var authResult = RequireUserId();
        if (authResult.IsFailure) return Result.Fail(authResult.Error!);

        var isInternalRequested = request.IsInternal;
        if (isInternalRequested && !authorizationService.HasPermission(CasePermissions.CreateInternalComment))
            return Result.Fail(Error.Forbidden("Not allowed."));

        var isInternal = isInternalRequested && authorizationService.HasPermission(CasePermissions.CreateInternalComment);

        var entity = await repository.GetScopedAsync(caseId, authResult.Value!, authorizationService.IsInternalUser, cancellationToken);
        if (entity is null)
            return Result.Fail(Error.NotFound("Investment case not found."));

        entity.Comments.Add(new CaseComment(
            caseId,
            request.Phase,
            authResult.Value!,
            senderRole: string.Join(",", userContext.Roles),
            request.Message,
            isRevisionRequest: false,
            isInternal: isInternal,
            parentId: request.ParentId));

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Ok();
    }

    public async Task<Result> AddCommentAttachmentAsync(Guid caseId, Guid commentId, string s3Key, string fileName, CancellationToken cancellationToken)
    {
        var authResult = RequireUserId();
        if (authResult.IsFailure) return Result.Fail(authResult.Error!);

        if (!authorizationService.HasPermission(CasePermissions.UploadCommentAttachments))
            return Result.Fail(Error.Forbidden("Not allowed."));

        var entity = await repository.GetScopedAsync(caseId, authResult.Value!, authorizationService.IsInternalUser, cancellationToken);
        if (entity is null)
            return Result.Fail(Error.NotFound("Investment case not found."));

        var comment = entity.Comments.FirstOrDefault(x => x.Id == commentId);
        if (comment is null)
            return Result.Fail(Error.NotFound("Comment not found."));

        var attachmentValidation = ValidateCommentAttachmentKey(entity.CaseNumber, commentId, s3Key, fileName);
        if (attachmentValidation.IsFailure) return attachmentValidation;

        var exists = await documentStorage.ExistsAsync(s3Key, cancellationToken);
        if (!exists) return Result.Fail(Error.Conflict("Uploaded file not found."));

        comment.AddAttachment(s3Key, fileName);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Ok();
    }

    public async Task<Result<IEnumerable<CaseDocumentDto>>> GetDocumentsAsync(Guid caseId, CancellationToken cancellationToken)
    {
        var authResult = RequireUserId();
        if (authResult.IsFailure) return Result<IEnumerable<CaseDocumentDto>>.Fail(authResult.Error!);

        var entity = await repository.GetScopedAsync(caseId, authResult.Value!, authorizationService.IsInternalUser, cancellationToken);
        if (entity is null)
            return Result<IEnumerable<CaseDocumentDto>>.Fail(Error.NotFound("Investment case not found."));

        var isInternalView = authorizationService.IsInternalUser;
        var docs = entity.Documents
            .Select(x =>
                isInternalView
                    ? (CaseDocumentDto)new CaseDocumentInternalDto(x.Id, x.CaseId, x.S3Key, x.FileName, x.MimeType, x.FileSize, x.Version, x.DocumentType, x.UploadedByUserId, x.UploadedAt)
                    : (CaseDocumentDto)new CaseDocumentApplicantDto(x.Id, x.CaseId, x.FileName, x.MimeType, x.FileSize, x.Version, x.DocumentType, x.UploadedAt))
            .OrderByDescending(x => x.UploadedAt);

        return Result<IEnumerable<CaseDocumentDto>>.Ok(docs);
    }

    public async Task<Result<IEnumerable<CaseWorkflowHistoryDto>>> GetHistoryAsync(Guid caseId, CancellationToken cancellationToken)
    {
        var authResult = RequireUserId();
        if (authResult.IsFailure) return Result<IEnumerable<CaseWorkflowHistoryDto>>.Fail(authResult.Error!);

        var entity = await repository.GetScopedAsync(caseId, authResult.Value!, authorizationService.IsInternalUser, cancellationToken);
        if (entity is null)
            return Result<IEnumerable<CaseWorkflowHistoryDto>>.Fail(Error.NotFound("Investment case not found."));

        var isInternalView = authorizationService.IsInternalUser;
        var history = entity.WorkflowHistory
            .Select(x =>
                isInternalView
                    ? (CaseWorkflowHistoryDto)new CaseWorkflowHistoryInternalDto(x.Id, x.CaseId, x.FromPhase, x.ToPhase, x.FromStatus, x.ToStatus, x.ChangedByUserId, x.ActorRole, x.Action, x.CorrelationId, x.Comment, x.CreatedAt)
                    : (CaseWorkflowHistoryDto)new CaseWorkflowHistoryApplicantDto(x.Id, x.CaseId, x.FromPhase, x.ToPhase, x.FromStatus, x.ToStatus, x.CreatedAt))
            .OrderBy(x => x.CreatedAt);

        return Result<IEnumerable<CaseWorkflowHistoryDto>>.Ok(history);
    }

    public async Task<Result> UpsertEvaluationAsync(Guid caseId, CaseEvaluationUpsertRequest request, CancellationToken cancellationToken)
    {
        var authResult = RequireUserId();
        if (authResult.IsFailure) return Result.Fail(authResult.Error!);

        if (!authorizationService.HasPermission(CasePermissions.UpsertEvaluations))
            return Result.Fail(Error.Forbidden("Not allowed."));

        var entity = await repository.GetScopedAsync(caseId, authResult.Value!, isInternalUser: true, cancellationToken);
        if (entity is null)
            return Result.Fail(Error.NotFound("Investment case not found."));

        var evaluation = entity.Evaluations.FirstOrDefault(x => x.Phase == request.Phase && x.ReviewerUserId == userContext.UserId);
        if (evaluation is null)
        {
            evaluation = new CaseEvaluation(caseId, request.Phase, authResult.Value!, "Reviewer", request.Notes);
            entity.Evaluations.Add(evaluation);
        }
        else
        {
            evaluation.UpdateNotes(request.Notes);
        }

        var items = request.Items.Select(x => new CaseEvaluationItem(evaluation.Id, x.Title, x.IsApproved, x.Comment)).ToList();
        evaluation.SetItems(items);

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Ok();
    }

    public async Task<Result<IEnumerable<CaseEvaluationDto>>> GetEvaluationsAsync(Guid caseId, CancellationToken cancellationToken)
    {
        var authResult = RequireUserId();
        if (authResult.IsFailure) return Result<IEnumerable<CaseEvaluationDto>>.Fail(authResult.Error!);

        if (!authorizationService.HasPermission(CasePermissions.ViewEvaluations))
            return Result<IEnumerable<CaseEvaluationDto>>.Fail(Error.NotFound("Investment case not found."));

        var entity = await repository.GetScopedAsync(caseId, authResult.Value!, isInternalUser: true, cancellationToken);
        if (entity is null)
            return Result<IEnumerable<CaseEvaluationDto>>.Fail(Error.NotFound("Investment case not found."));

        var evaluations = entity.Evaluations
            .Select(x => new CaseEvaluationDto(
                x.Id,
                x.CaseId,
                x.Phase,
                x.ReviewerUserId,
                x.ReviewerRole,
                x.Notes,
                x.Items.Select(i => new CaseEvaluationItemDto(i.Id, i.Title, i.IsApproved, i.Comment)),
                x.CreatedAt))
            .OrderByDescending(x => x.CreatedAt);

        return Result<IEnumerable<CaseEvaluationDto>>.Ok(evaluations);
    }

    public async Task<Result<IEnumerable<InvestmentCaseDto>>> SearchAsync(CaseSearchRequest request, CancellationToken cancellationToken)
    {
        var authResult = RequireUserId();
        if (authResult.IsFailure) return Result<IEnumerable<InvestmentCaseDto>>.Fail(authResult.Error!);

        var results = await repository.SearchScopedAsync(
            request.CaseNumber,
            request.ApplicantUserId,
            request.Phase,
            request.Status,
            request.FromDate,
            request.ToDate,
            request.Page,
            request.PageSize,
            authResult.Value!,
            authorizationService.IsInternalUser,
            cancellationToken);

        var dtos = results.Select(x => ToDto(x, clock.UtcNow, authorizationService.IsInternalUser));
        return Result<IEnumerable<InvestmentCaseDto>>.Ok(dtos);
    }

    public async Task<Result<PresignUploadResponse>> PresignDocumentUploadAsync(Guid caseId, PresignUploadRequest request, CancellationToken cancellationToken)
    {
        var authResult = RequireUserId();
        if (authResult.IsFailure) return Result<PresignUploadResponse>.Fail(authResult.Error!);

        var entity = await repository.GetScopedAsync(caseId, authResult.Value!, authorizationService.IsInternalUser, cancellationToken);
        if (entity is null)
            return Result<PresignUploadResponse>.Fail(Error.NotFound("Investment case not found."));

        var validation = ValidatePresignRequest(entity, request);
        if (validation.IsFailure) return Result<PresignUploadResponse>.Fail(validation.Error!);

        var version = entity.Documents.Where(x => x.DocumentType == request.DocumentType).Select(x => x.Version).DefaultIfEmpty(0).Max() + 1;
        var ext = GetSafeExtension(request.FileName);
        var s3Key = $"cases/{entity.CaseNumber}/{request.DocumentType}/{version}{ext}";

        var (url, expiresAt) = await documentStorage.PresignUploadAsync(s3Key, request.MimeType, TimeSpan.FromMinutes(15), cancellationToken);
        return Result<PresignUploadResponse>.Ok(new PresignUploadResponse(s3Key, url, expiresAt, version));
    }

    public async Task<Result<CaseDocumentDto>> ConfirmDocumentUploadedAsync(Guid caseId, string s3Key, CancellationToken cancellationToken)
    {
        var authResult = RequireUserId();
        if (authResult.IsFailure) return Result<CaseDocumentDto>.Fail(authResult.Error!);

        var entity = await repository.GetScopedAsync(caseId, authResult.Value!, authorizationService.IsInternalUser, cancellationToken);
        if (entity is null)
            return Result<CaseDocumentDto>.Fail(Error.NotFound("Investment case not found."));

        var keyValidation = ValidateAndNormalizeDocumentKey(entity.CaseNumber, s3Key);
        if (keyValidation.IsFailure) return Result<CaseDocumentDto>.Fail(keyValidation.Error!);

        s3Key = keyValidation.Value!;

        if (entity.Documents.Any(x => string.Equals(x.S3Key, s3Key, StringComparison.Ordinal)))
            return Result<CaseDocumentDto>.Fail(Error.Conflict("Document already exists."));

        var docType = ExtractDocumentTypeFromKey(s3Key);
        var authorization = EnsureCanConfirmDocument(entity, docType);
        if (authorization.IsFailure) return Result<CaseDocumentDto>.Fail(authorization.Error!);

        var exists = await documentStorage.ExistsAsync(s3Key, cancellationToken);
        if (!exists) return Result<CaseDocumentDto>.Fail(Error.Conflict("Uploaded file not found."));

        var ext = Path.GetExtension(s3Key);
        var expectedVersion = entity.Documents.Where(x => x.DocumentType == docType).Select(x => x.Version).DefaultIfEmpty(0).Max() + 1;
        var expectedKey = $"cases/{entity.CaseNumber}/{docType}/{expectedVersion}{ext}";
        if (!string.Equals(s3Key, expectedKey, StringComparison.Ordinal))
            return Result<CaseDocumentDto>.Fail(Error.Forbidden("Not allowed."));

        var version = expectedVersion;
        var document = entity.AddDocument(
            s3Key,
            Path.GetFileName(s3Key),
            "application/octet-stream",
            0,
            version,
            docType,
            authResult.Value!);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<CaseDocumentDto>.Ok(
            authorizationService.IsInternalUser
                ? new CaseDocumentInternalDto(document.Id, document.CaseId, document.S3Key, document.FileName, document.MimeType, document.FileSize, document.Version, document.DocumentType, document.UploadedByUserId, document.UploadedAt)
                : new CaseDocumentApplicantDto(document.Id, document.CaseId, document.FileName, document.MimeType, document.FileSize, document.Version, document.DocumentType, document.UploadedAt));
    }

    public async Task<Result<PresignDownloadResponse>> PresignDocumentDownloadAsync(Guid caseId, Guid documentId, CancellationToken cancellationToken)
    {
        var authResult = RequireUserId();
        if (authResult.IsFailure) return Result<PresignDownloadResponse>.Fail(authResult.Error!);

        if (!authorizationService.HasPermission(CasePermissions.DownloadDocuments))
            return Result<PresignDownloadResponse>.Fail(Error.Forbidden("Not allowed."));

        var entity = await repository.GetScopedAsync(caseId, authResult.Value!, authorizationService.IsInternalUser, cancellationToken);
        if (entity is null)
            return Result<PresignDownloadResponse>.Fail(Error.NotFound("Investment case not found."));

        var document = entity.Documents.FirstOrDefault(x => x.Id == documentId);
        if (document is null)
            return Result<PresignDownloadResponse>.Fail(Error.NotFound("Document not found."));

        var (url, expiresAt) = await documentStorage.PresignDownloadAsync(document.S3Key, TimeSpan.FromMinutes(10), cancellationToken);
        return Result<PresignDownloadResponse>.Ok(new PresignDownloadResponse(url, expiresAt, document.FileName));
    }

    private Result<string> RequireUserId()
    {
        var auth = authorizationService.EnsureAuthenticated();
        if (auth.IsFailure) return Result<string>.Fail(auth.Error!);
        return Result<string>.Ok(authorizationService.UserId!);
    }

    private static InvestmentCaseDto ToDto(InvestmentCase entity, DateTimeOffset now, bool isInternalView)
        => isInternalView ? ToInternalDto(entity, now) : ToApplicantDto(entity, now);

    private static InvestmentCaseApplicantDto ToApplicantDto(InvestmentCase entity, DateTimeOffset now)
        => new(
            entity.Id,
            entity.CaseNumber,
            entity.ApplicantType,
            entity.CurrentPhase,
            entity.CurrentStatus,
            entity.CreatedAt,
            entity.UpdatedAt ?? now,
            entity.CompletedAt,
            entity.Company is null
                ? null
                : new CompanyProfileDto(
                    entity.Company.Name,
                    entity.Company.EconomicCode,
                    entity.Company.RegistrationNumber,
                    entity.Company.NationalId,
                    entity.Company.PhoneNumber,
                    entity.Company.Address,
                    entity.Company.City,
                    entity.Company.Province,
                    entity.Company.PostalCode));

    private static InvestmentCaseInternalDto ToInternalDto(InvestmentCase entity, DateTimeOffset now)
        => new(
            entity.Id,
            entity.CaseNumber,
            entity.ApplicantUserId,
            entity.ApplicantType,
            entity.CurrentPhase,
            entity.CurrentStatus,
            entity.WorkflowInstanceId,
            entity.CreatedAt,
            entity.UpdatedAt ?? now,
            entity.CompletedAt,
            BitConverter.GetBytes(entity.RowVersion),
            entity.Company is null
                ? null
                : new CompanyProfileDto(
                    entity.Company.Name,
                    entity.Company.EconomicCode,
                    entity.Company.RegistrationNumber,
                    entity.Company.NationalId,
                    entity.Company.PhoneNumber,
                    entity.Company.Address,
                    entity.Company.City,
                    entity.Company.Province,
                    entity.Company.PostalCode));

    private Result ValidatePresignRequest(InvestmentCase entity, PresignUploadRequest request)
    {
        if (!IsSafeClientFileName(request.FileName))
            return Result.Fail(Error.Validation("Invalid file name."));

        var ext = GetSafeExtension(request.FileName);
        if (!IsAllowedUpload(request.MimeType, ext))
            return Result.Fail(Error.Validation("File type is not allowed."));

        if (authorizationService.IsInternalUser)
        {
            if (request.DocumentType is DocumentType.PreContract or DocumentType.FinalContract or DocumentType.SignedContract)
            {
                if (!authorizationService.HasPermission(CasePermissions.ManageContracts))
                    return Result.Fail(Error.Forbidden("Not allowed."));

                if (request.DocumentType is DocumentType.PreContract && entity.CurrentStatus != CaseStatus.WaitingPreliminaryContract)
                    return Result.Fail(Error.Conflict("Document upload is not allowed in the current status."));

                if (request.DocumentType is DocumentType.FinalContract && entity.CurrentStatus is not (CaseStatus.ContractDrafting or CaseStatus.WaitingContractSignature))
                    return Result.Fail(Error.Conflict("Document upload is not allowed in the current status."));

                if (request.DocumentType is DocumentType.SignedContract && entity.CurrentStatus != CaseStatus.WaitingSignedContractUpload)
                    return Result.Fail(Error.Conflict("Document upload is not allowed in the current status."));

                return Result.Ok();
            }

            if (request.DocumentType is DocumentType.PaymentReceipt)
            {
                if (!authorizationService.HasPermission(CasePermissions.ManagePayments))
                    return Result.Fail(Error.Forbidden("Not allowed."));

                if (entity.CurrentStatus != CaseStatus.WaitingPayment)
                    return Result.Fail(Error.Conflict("Document upload is not allowed in the current status."));

                return Result.Ok();
            }

            if (!authorizationService.HasPermission(CasePermissions.UploadDocuments))
                return Result.Fail(Error.Forbidden("Not allowed."));

            return Result.Ok();
        }

        if (!authorizationService.HasPermission(CasePermissions.UploadDocuments))
            return Result.Fail(Error.Forbidden("Not allowed."));

        if (request.DocumentType is DocumentType.PreContract or DocumentType.FinalContract or DocumentType.SignedContract or DocumentType.PaymentReceipt)
            return Result.Fail(Error.Forbidden("Not allowed."));

        if (entity.CurrentStatus is not (CaseStatus.Draft or CaseStatus.DataEntry1 or CaseStatus.DataEntry2))
            return Result.Fail(Error.Conflict("Document upload is not allowed in the current status."));

        return Result.Ok();
    }

    private Result EnsureCanConfirmDocument(InvestmentCase entity, DocumentType documentType)
    {
        if (!authorizationService.IsInternalUser)
        {
            if (documentType is DocumentType.PreContract or DocumentType.FinalContract or DocumentType.SignedContract or DocumentType.PaymentReceipt)
                return Result.Fail(Error.Forbidden("Not allowed."));

            if (entity.CurrentStatus is not (CaseStatus.Draft or CaseStatus.DataEntry1 or CaseStatus.DataEntry2))
                return Result.Fail(Error.Conflict("Document confirmation is not allowed in the current status."));

            return Result.Ok();
        }

        if (documentType is DocumentType.PreContract or DocumentType.FinalContract or DocumentType.SignedContract)
        {
            if (!authorizationService.HasPermission(CasePermissions.ManageContracts))
                return Result.Fail(Error.Forbidden("Not allowed."));

            if (documentType is DocumentType.PreContract && entity.CurrentStatus != CaseStatus.WaitingPreliminaryContract)
                return Result.Fail(Error.Conflict("Document confirmation is not allowed in the current status."));

            if (documentType is DocumentType.FinalContract && entity.CurrentStatus is not (CaseStatus.ContractDrafting or CaseStatus.WaitingContractSignature))
                return Result.Fail(Error.Conflict("Document confirmation is not allowed in the current status."));

            if (documentType is DocumentType.SignedContract && entity.CurrentStatus != CaseStatus.WaitingSignedContractUpload)
                return Result.Fail(Error.Conflict("Document confirmation is not allowed in the current status."));

            return Result.Ok();
        }

        if (documentType is DocumentType.PaymentReceipt)
        {
            if (!authorizationService.HasPermission(CasePermissions.ManagePayments))
                return Result.Fail(Error.Forbidden("Not allowed."));

            if (entity.CurrentStatus != CaseStatus.WaitingPayment)
                return Result.Fail(Error.Conflict("Document confirmation is not allowed in the current status."));

            return Result.Ok();
        }

        return authorizationService.HasPermission(CasePermissions.UploadDocuments) ? Result.Ok() : Result.Fail(Error.Forbidden("Not allowed."));
    }

    private static Result<string> ValidateAndNormalizeDocumentKey(string caseNumber, string s3Key)
    {
        if (string.IsNullOrWhiteSpace(s3Key))
            return Result<string>.Fail(Error.Validation("Invalid document key."));

        if (s3Key.Contains('\\') || s3Key.Contains("..", StringComparison.Ordinal))
            return Result<string>.Fail(Error.Validation("Invalid document key."));

        if (!s3Key.StartsWith($"cases/{caseNumber}/", StringComparison.Ordinal))
            return Result<string>.Fail(Error.Forbidden("Not allowed."));

        var parts = s3Key.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 4)
            return Result<string>.Fail(Error.Validation("Invalid document key."));

        if (!Enum.TryParse<DocumentType>(parts[2], ignoreCase: true, out _))
            return Result<string>.Fail(Error.Validation("Invalid document key."));

        var file = parts[3];
        if (file.Length > 128)
            return Result<string>.Fail(Error.Validation("Invalid document key."));

        var ext = Path.GetExtension(file);
        if (string.IsNullOrWhiteSpace(ext) || ext.Length > 10)
            return Result<string>.Fail(Error.Validation("Invalid document key."));

        var fileNameWithoutExt = Path.GetFileNameWithoutExtension(file);
        if (!int.TryParse(fileNameWithoutExt, out _))
            return Result<string>.Fail(Error.Validation("Invalid document key."));

        return Result<string>.Ok(s3Key);
    }

    private static DocumentType ExtractDocumentTypeFromKey(string s3Key)
    {
        var parts = s3Key.Split('/', StringSplitOptions.RemoveEmptyEntries);
        return Enum.TryParse<DocumentType>(parts[2], ignoreCase: true, out var docType) ? docType : DocumentType.Other;
    }

    private static bool IsSafeClientFileName(string fileName)
        => !string.IsNullOrWhiteSpace(fileName) && string.Equals(Path.GetFileName(fileName), fileName, StringComparison.Ordinal);

    private static string GetSafeExtension(string fileName)
    {
        var ext = Path.GetExtension(Path.GetFileName(fileName));
        return string.IsNullOrWhiteSpace(ext) ? string.Empty : ext.ToLowerInvariant();
    }

    private static bool IsAllowedUpload(string mimeType, string ext)
    {
        if (string.IsNullOrWhiteSpace(mimeType) || string.IsNullOrWhiteSpace(ext))
            return false;

        return (mimeType.Equals("application/pdf", StringComparison.OrdinalIgnoreCase) && ext == ".pdf")
               || (mimeType.Equals("image/jpeg", StringComparison.OrdinalIgnoreCase) && (ext == ".jpg" || ext == ".jpeg"))
               || (mimeType.Equals("image/png", StringComparison.OrdinalIgnoreCase) && ext == ".png");
    }

    private static Result ValidateCommentAttachmentKey(string caseNumber, Guid commentId, string s3Key, string fileName)
    {
        if (string.IsNullOrWhiteSpace(s3Key))
            return Result.Fail(Error.Validation("Invalid attachment key."));

        if (!IsSafeClientFileName(fileName))
            return Result.Fail(Error.Validation("Invalid file name."));

        if (s3Key.Contains('\\') || s3Key.Contains("..", StringComparison.Ordinal))
            return Result.Fail(Error.Validation("Invalid attachment key."));

        var prefix = $"cases/{caseNumber}/comments/{commentId:D}/";
        if (!s3Key.StartsWith(prefix, StringComparison.Ordinal))
            return Result.Fail(Error.Forbidden("Not allowed."));

        return Result.Ok();
    }
}
