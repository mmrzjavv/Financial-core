using Core.Application.Common;
using BuildingBlocks.Application.Errors;
using BuildingBlocks.Application.Results;
using BuildingBlocks.Application.Abstractions;
using BuildingBlocks.Domain.Abstractions;
using BuildingBlocks.Observability.Correlation;
using Core.Application.Authorization;
using Core.Application.Abstractions;
using Core.Application.Logging;
using Core.Application.Mappers;
using Microsoft.EntityFrameworkCore;
using Core.Application.DTOs;
using Core.Application.Requests;
using Core.Application.Responses;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Services.CoreService.Core.Domain.Constants;
using Services.CoreService.Core.Domain.Entities;
using Services.CoreService.Core.Domain.Enums;
using Services.CoreService.Core.Domain.Identity.Entities;


namespace Core.Application.Services;

public sealed class InvestmentCaseAppService(
    ICoreUnitOfWork unitOfWork,
    ICoreDbContext dbContext,
    ICaseStateManager stateManager,
    ICaseWorkflowOrchestrator workflowOrchestrator,
    ICaseNumberGenerator caseNumberGenerator,
    IDocumentStorage documentStorage,
    BuildingBlocks.Domain.Abstractions.IClock clock,
    IUserContext userContext,
    ICaseAuthorizationService authorizationService,
    ICaseDtoMapper caseDtoMapper,
    IHttpContextAccessor httpContextAccessor,
    ILogger<InvestmentCaseAppService> logger) : IInvestmentCaseAppService
{
    public async Task<Result<InvestmentCaseDto>> CreateAsync(CreateInvestmentCaseRequest request, CancellationToken cancellationToken)
    {
        var authResult = RequireUserId();
        if (authResult.IsFailure)
        {
            ApplicationLog.Blocked(logger, "CreateInvestmentCase", "user is not authenticated");
            return Result<InvestmentCaseDto>.Fail(authResult.Error!);
        }

        ApplicationLog.Started(logger, "CreateInvestmentCase", authResult.Value);

        if (!authorizationService.HasPermission(CasePermissions.Create))
        {
            ApplicationLog.Blocked(logger, "CreateInvestmentCase", "missing Create permission", authResult.Value);
            return Result<InvestmentCaseDto>.Fail(Error.Forbidden(ApiMessages.NotAllowed));
        }

        var now = clock.UtcNow;
        var caseNumber = await caseNumberGenerator.GenerateAsync(cancellationToken);
        var entity = new InvestmentCase(caseNumber, authResult.Value!, request.ApplicantType);
        Company? linkedCompany = null;

        if (request.ApplicantType == ApplicantType.Company)
        {
            if (request.CompanyId is null || request.CompanyId == Guid.Empty)
                return Result<InvestmentCaseDto>.Fail(Error.Validation(ApiMessages.CompanyRequiredForCompanyApplicant));

            if (!Guid.TryParse(authResult.Value, out var userId))
                return Result<InvestmentCaseDto>.Fail(Error.Unauthorized(ApiMessages.AuthenticationRequired));

            linkedCompany = await unitOfWork.Companies.FirstOrDefaultAsync(
                c => c.Id == request.CompanyId.Value,
                asNoTracking: true,
                cancellationToken);

            if (linkedCompany is null)
                return Result<InvestmentCaseDto>.Fail(Error.NotFound(ApiMessages.CompanyNotFound));

            if (linkedCompany.OwnerUserId != userId)
                return Result<InvestmentCaseDto>.Fail(Error.Forbidden(ApiMessages.CompanyAccessDenied));

            entity.AssignCompany(linkedCompany.Id);
        }

        var workflowInstanceId = await workflowOrchestrator.StartAsync(entity.Id, cancellationToken);
        entity.AttachWorkflowInstance(workflowInstanceId);

        await unitOfWork.InvestmentCases.AddAsync(entity, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        ApplicationLog.Completed(logger,
            "User {UserId} created investment case {CaseId} ({CaseNumber}) as {ApplicantType}; workflow instance {WorkflowInstanceId} started",
            authResult.Value, entity.Id, entity.CaseNumber, request.ApplicantType, workflowInstanceId);

        return Result<InvestmentCaseDto>.Ok(caseDtoMapper.MapCase(entity, now, isInternalView: false, linkedCompany));
    }

    public async Task<Result<InvestmentCaseDto>> GetAsync(Guid caseId, CancellationToken cancellationToken)
    {
        var authResult = RequireUserId();
        if (authResult.IsFailure)
        {
            ApplicationLog.Blocked(logger, "GetInvestmentCase", "user is not authenticated", caseId: caseId);
            return Result<InvestmentCaseDto>.Fail(authResult.Error!);
        }

        ApplicationLog.Started(logger, "GetInvestmentCase", authResult.Value, caseId);

        var entity = await unitOfWork.InvestmentCases.GetScopedAsync(caseId, authResult.Value!, authorizationService.IsInternalUser, cancellationToken);
        if (entity is null)
        {
            ApplicationLog.Blocked(logger, "GetInvestmentCase", "case not found or access denied", authResult.Value, caseId);
            return Result<InvestmentCaseDto>.Fail(Error.NotFound(ApiMessages.CaseNotFound));
        }

        ApplicationLog.Completed(logger,
            "User {UserId} loaded case {CaseId} ({CaseNumber}) — phase {Phase}, status {Status}",
            authResult.Value, entity.Id, entity.CaseNumber, entity.CurrentPhase, entity.CurrentStatus);

        return Result<InvestmentCaseDto>.Ok(caseDtoMapper.MapCase(entity, clock.UtcNow, authorizationService.IsInternalUser));
    }

    public async Task<Result> UpdateDataEntry1Async(Guid caseId, UpdateDataEntry1Request request, CancellationToken cancellationToken)
    {
        var authResult = RequireUserId();
        if (authResult.IsFailure) return Result.Fail(authResult.Error!);

        ApplicationLog.Started(logger, "UpdateDataEntry1", authResult.Value, caseId);

        if (!userContext.Roles.Contains(SystemRoles.Applicant))
        {
            ApplicationLog.Blocked(logger, "UpdateDataEntry1", "only applicants may edit data entry 1", authResult.Value, caseId);
            return Result.Fail(Error.Forbidden(ApiMessages.NotAllowed));
        }

        var caseStatus = await dbContext.InvestmentCases
            .AsNoTracking()
            .Where(x => x.Id == caseId && x.ApplicantUserId == authResult.Value)
            .Select(x => x.CurrentStatus)
            .FirstOrDefaultAsync(cancellationToken);

        if (caseStatus == default)
        {
            ApplicationLog.Blocked(logger, "UpdateDataEntry1", "case not found", authResult.Value, caseId);
            return Result.Fail(Error.NotFound(ApiMessages.CaseNotFound));
        }

        if (caseStatus is not (CaseStatus.Draft or CaseStatus.DataEntry1))
        {
            ApplicationLog.Blocked(logger, "UpdateDataEntry1", $"case status is {caseStatus}", authResult.Value, caseId);
            return Result.Fail(Error.Conflict(ApiMessages.DataEntry1NotEditable));
        }

        var dataEntry = await dbContext.DataEntry1
            .FirstOrDefaultAsync(x => x.CaseId == caseId, cancellationToken);

        if (dataEntry is null)
        {
            dataEntry = new InvestmentCaseDataEntry1(
                caseId,
                request.StartupTitle,
                request.BusinessDescription,
                request.RequestedAmount,
                request.TeamSize,
                request.Website);
            await dbContext.DataEntry1.AddAsync(dataEntry, cancellationToken);
        }
        else
        {
            dataEntry.Update(
                request.StartupTitle,
                request.BusinessDescription,
                request.RequestedAmount,
                request.TeamSize,
                request.Website,
                request.Country,
                request.City,
                industry: null);
        }

        var now = clock.UtcNow;
        await dbContext.InvestmentCases
            .Where(x => x.Id == caseId)
            .ExecuteUpdateAsync(setters => setters.SetProperty(x => x.UpdatedAt, now), cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        ApplicationLog.Completed(logger,
            "User {UserId} saved data entry 1 on case {CaseId} (startup: {StartupTitle})",
            authResult.Value, caseId, request.StartupTitle);

        return Result.Ok();
    }

    public async Task<Result> UpdateDataEntry2Async(Guid caseId, UpdateDataEntry2Request request, CancellationToken cancellationToken)
    {
        var authResult = RequireUserId();
        if (authResult.IsFailure) return Result.Fail(authResult.Error!);

        ApplicationLog.Started(logger, "UpdateDataEntry2", authResult.Value, caseId);

        if (!userContext.Roles.Contains(SystemRoles.Applicant))
        {
            ApplicationLog.Blocked(logger, "UpdateDataEntry2", "only applicants may edit data entry 2", authResult.Value, caseId);
            return Result.Fail(Error.Forbidden(ApiMessages.NotAllowed));
        }

        var caseStatus = await dbContext.InvestmentCases
            .AsNoTracking()
            .Where(x => x.Id == caseId && x.ApplicantUserId == authResult.Value)
            .Select(x => x.CurrentStatus)
            .FirstOrDefaultAsync(cancellationToken);

        if (caseStatus == default)
        {
            ApplicationLog.Blocked(logger, "UpdateDataEntry2", "case not found", authResult.Value, caseId);
            return Result.Fail(Error.NotFound(ApiMessages.CaseNotFound));
        }

        if (caseStatus is not CaseStatus.DataEntry2)
        {
            ApplicationLog.Blocked(logger, "UpdateDataEntry2", $"case status is {caseStatus}", authResult.Value, caseId);
            return Result.Fail(Error.Conflict(ApiMessages.DataEntry2NotEditable));
        }

        var dataEntry = await dbContext.DataEntry2
            .FirstOrDefaultAsync(x => x.CaseId == caseId, cancellationToken);

        if (dataEntry is null)
        {
            dataEntry = new InvestmentCaseDataEntry2(
                caseId,
                request.MarketAnalysis,
                request.RevenueModel,
                request.CompetitiveAdvantage,
                request.FinancialProjection ?? string.Empty);
            await dbContext.DataEntry2.AddAsync(dataEntry, cancellationToken);
        }
        else
        {
            dataEntry.Update(
                request.MarketAnalysis,
                request.RevenueModel,
                request.CompetitiveAdvantage,
                request.FinancialProjection ?? string.Empty,
                risks: null,
                goToMarketStrategy: null);
        }

        var now = clock.UtcNow;
        await dbContext.InvestmentCases
            .Where(x => x.Id == caseId)
            .ExecuteUpdateAsync(setters => setters.SetProperty(x => x.UpdatedAt, now), cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        ApplicationLog.Completed(logger,
            "User {UserId} saved data entry 2 on case {CaseId}",
            authResult.Value, caseId);

        return Result.Ok();
    }

    public async Task<Result> UpdateFinancialWorksheetAsync(Guid caseId, UpdateFinancialWorksheetRequest request, CancellationToken cancellationToken)
    {
        var authResult = RequireUserId();
        if (authResult.IsFailure) return Result.Fail(authResult.Error!);

        ApplicationLog.Started(logger, "UpdateFinancialWorksheet", authResult.Value, caseId);

        if (!authorizationService.HasPermission(CasePermissions.ManageFinancialWorksheet))
        {
            ApplicationLog.Blocked(logger, "UpdateFinancialWorksheet", "missing ManageFinancialWorksheet permission", authResult.Value, caseId);
            return Result.Fail(Error.Forbidden(ApiMessages.NotAllowed));
        }

        var entity = await unitOfWork.InvestmentCases.GetScopedAsync(caseId, authResult.Value!, authorizationService.IsInternalUser, cancellationToken);
        if (entity is null)
        {
            ApplicationLog.Blocked(logger, "UpdateFinancialWorksheet", "case not found", authResult.Value, caseId);
            return Result.Fail(Error.NotFound(ApiMessages.CaseNotFound));
        }

        if (entity.CurrentStatus is not CaseStatus.WaitingFinancialWorksheet)
        {
            ApplicationLog.Blocked(logger, "UpdateFinancialWorksheet", $"case status is {entity.CurrentStatus}", authResult.Value, caseId);
            return Result.Fail(Error.Conflict(ApiMessages.FinancialWorksheetNotEditable));
        }

        entity.UpsertFinancialWorksheet(
            request.BankName,
            request.Iban,
            request.ApprovedAmount ?? 0m,
            request.PaymentSchedule ?? string.Empty,
            request.Notes);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        ApplicationLog.Completed(logger,
            "User {UserId} updated financial worksheet on case {CaseId} ({CaseNumber})",
            authResult.Value, caseId, entity.CaseNumber);

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
            return Result.Fail(Error.Forbidden(ApiMessages.NotAllowed));

        return await ApplyTransitionAsync(caseId, WorkflowAction.Approve, comment, ct);
    }

    public async Task<Result> ApproveSecondaryValuationAsync(Guid caseId, string? comment, CancellationToken ct)
    {
        if (!authorizationService.HasPermission(CasePermissions.ManageValuations))
            return Result.Fail(Error.Forbidden(ApiMessages.NotAllowed));

        return await ApplyTransitionAsync(caseId, WorkflowAction.Approve, comment, ct);
    }

    public async Task<Result> UploadPreliminaryContractAsync(Guid caseId, string s3Key, CancellationToken ct)
    {
        if (!authorizationService.HasPermission(CasePermissions.ManageContracts))
            return Result.Fail(Error.Forbidden(ApiMessages.NotAllowed));

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
            return Result.Fail(Error.Forbidden(ApiMessages.NotAllowed));

        return await ApplyTransitionAsync(caseId, WorkflowAction.FinalizeContractDraft, comment, ct);
    }

    public async Task<Result> ConfirmSignatureAsync(Guid caseId, string? comment, CancellationToken ct)
    {
        if (!authorizationService.HasPermission(CasePermissions.ManageContracts))
            return Result.Fail(Error.Forbidden(ApiMessages.NotAllowed));

        return await ApplyTransitionAsync(caseId, WorkflowAction.ConfirmSignature, comment, ct);
    }

    public async Task<Result> UploadSignedContractAsync(Guid caseId, string s3Key, CancellationToken ct)
    {
        if (!authorizationService.HasPermission(CasePermissions.ManageContracts))
            return Result.Fail(Error.Forbidden(ApiMessages.NotAllowed));

        var result = await ConfirmDocumentUploadedAsync(caseId, s3Key, ct);
        if (result.IsFailure) return Result.Fail(result.Error!);
        return await ApplyTransitionAsync(caseId, WorkflowAction.UploadSignedContract, "Signed contract uploaded", ct);
    }

    public async Task<Result> SubmitFinancialWorksheetAsync(Guid caseId, string? comment, CancellationToken ct)
    {
        if (!authorizationService.HasPermission(CasePermissions.ManageFinancialWorksheet))
            return Result.Fail(Error.Forbidden(ApiMessages.NotAllowed));

        return await ApplyTransitionAsync(caseId, WorkflowAction.SubmitFinancialWorksheet, comment, ct);
    }

    public async Task<Result> ApproveFinancialWorksheetAsync(Guid caseId, string? comment, CancellationToken ct)
    {
        if (!authorizationService.HasPermission(CasePermissions.ManageFinancialWorksheet))
            return Result.Fail(Error.Forbidden(ApiMessages.NotAllowed));

        return await ApplyTransitionAsync(caseId, WorkflowAction.ApproveFinancialWorksheet, comment, ct);
    }

    public async Task<Result> RequestFinancialWorksheetRevisionAsync(Guid caseId, string message, CancellationToken ct)
    {
        if (!authorizationService.HasPermission(CasePermissions.ManageFinancialWorksheet))
            return Result.Fail(Error.Forbidden(ApiMessages.NotAllowed));

        return await ApplyTransitionAsync(caseId, WorkflowAction.RequestRevision, message, ct);
    }

    public async Task<Result> ConfirmPaymentAsync(Guid caseId, Guid paymentId, CancellationToken ct)
    {
        var authResult = RequireUserId();
        if (authResult.IsFailure) return Result.Fail(authResult.Error!);

        ApplicationLog.Started(logger, "ConfirmPayment", authResult.Value, caseId);

        if (!authorizationService.HasPermission(CasePermissions.ManagePayments))
        {
            ApplicationLog.Blocked(logger, "ConfirmPayment", "missing ManagePayments permission", authResult.Value, caseId);
            return Result.Fail(Error.Forbidden(ApiMessages.NotAllowed));
        }

        var entity = await unitOfWork.InvestmentCases.GetScopedAsync(caseId, authResult.Value!, isInternalUser: true, ct);
        if (entity is null)
        {
            ApplicationLog.Blocked(logger, "ConfirmPayment", "case not found", authResult.Value, caseId);
            return Result.Fail(Error.NotFound(ApiMessages.CaseNotFound));
        }

        var payment = entity.Payments.FirstOrDefault(x => x.Id == paymentId);
        if (payment is null)
        {
            ApplicationLog.Blocked(logger, "ConfirmPayment", $"payment {paymentId} not found", authResult.Value, caseId);
            return Result.Fail(Error.NotFound(ApiMessages.PaymentNotFound));
        }

        payment.Update(payment.Amount, payment.PaymentDate, payment.TransactionNumber, payment.ReceiptS3Key, payment.Notes, payment.Method, PaymentStatus.Completed);

        entity.CheckPaymentCompletion(authResult.Value!);
        await unitOfWork.SaveChangesAsync(ct);

        ApplicationLog.Completed(logger,
            "User {UserId} confirmed payment {PaymentId} on case {CaseId} ({CaseNumber}); case status is now {Status}",
            authResult.Value, paymentId, caseId, entity.CaseNumber, entity.CurrentStatus);

        return Result.Ok();
    }

    public async Task<Result> CancelPaymentAsync(Guid caseId, Guid paymentId, CancellationToken ct)
    {
        var authResult = RequireUserId();
        if (authResult.IsFailure) return Result.Fail(authResult.Error!);

        ApplicationLog.Started(logger, "CancelPayment", authResult.Value, caseId);

        if (!authorizationService.HasPermission(CasePermissions.ManagePayments))
        {
            ApplicationLog.Blocked(logger, "CancelPayment", "missing ManagePayments permission", authResult.Value, caseId);
            return Result.Fail(Error.Forbidden(ApiMessages.NotAllowed));
        }

        var entity = await unitOfWork.InvestmentCases.GetScopedAsync(caseId, authResult.Value!, isInternalUser: true, ct);
        if (entity is null)
        {
            ApplicationLog.Blocked(logger, "CancelPayment", "case not found", authResult.Value, caseId);
            return Result.Fail(Error.NotFound(ApiMessages.CaseNotFound));
        }

        var payment = entity.Payments.FirstOrDefault(x => x.Id == paymentId);
        if (payment is null)
        {
            ApplicationLog.Blocked(logger, "CancelPayment", $"payment {paymentId} not found", authResult.Value, caseId);
            return Result.Fail(Error.NotFound(ApiMessages.PaymentNotFound));
        }

        payment.Update(payment.Amount, payment.PaymentDate, payment.TransactionNumber, payment.ReceiptS3Key, payment.Notes, payment.Method, PaymentStatus.Cancelled);
        await unitOfWork.SaveChangesAsync(ct);

        ApplicationLog.Completed(logger,
            "User {UserId} cancelled payment {PaymentId} on case {CaseId} ({CaseNumber})",
            authResult.Value, paymentId, caseId, entity.CaseNumber);

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
        if (authResult.IsFailure)
        {
            ApplicationLog.Blocked(logger, $"Workflow:{action}", "user is not authenticated", caseId: caseId);
            return Result.Fail(authResult.Error!);
        }

        var actorRole = ResolveActorRole();
        ApplicationLog.Started(logger, $"Workflow:{action}", authResult.Value, caseId);

        var entity = await unitOfWork.InvestmentCases.GetScopedAsync(caseId, authResult.Value!, authorizationService.IsInternalUser, ct);
        if (entity is null)
        {
            ApplicationLog.Blocked(logger, $"Workflow:{action}", "case not found or access denied", authResult.Value, caseId);
            return Result.Fail(Error.NotFound(ApiMessages.CaseNotFound));
        }

        var statusBefore = entity.CurrentStatus;
        var phaseBefore = entity.CurrentPhase;

        var correlationId = ResolveCorrelationGuid(httpContextAccessor.HttpContext);
        var transition = await stateManager.TransitionAsync(entity, action, authResult.Value!, actorRole, comment, correlationId);
        if (transition.IsFailure)
        {
            ApplicationLog.Blocked(logger, $"Workflow:{action}",
                transition.Error?.Message ?? "transition rejected by state machine",
                authResult.Value, caseId);
            return transition;
        }

        await workflowOrchestrator.SignalAsync(
            caseId,
            WorkflowSignals.StatusChanged,
            new { status = entity.CurrentStatus, action = action.ToString() },
            ct);

        await unitOfWork.SaveChangesAsync(ct);

        ApplicationLog.Completed(logger,
            "User {UserId} (role {Role}) applied {Action} on case {CaseId} ({CaseNumber}): {PhaseBefore}/{StatusBefore} → {PhaseAfter}/{StatusAfter}",
            authResult.Value, actorRole, action, caseId, entity.CaseNumber,
            phaseBefore, statusBefore, entity.CurrentPhase, entity.CurrentStatus);

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

        ApplicationLog.Started(logger, "RecordValuation", authResult.Value, caseId);

        if (!authorizationService.HasPermission(CasePermissions.ManageValuations))
        {
            ApplicationLog.Blocked(logger, "RecordValuation", "missing ManageValuations permission", authResult.Value, caseId);
            return Result.Fail(Error.Forbidden(ApiMessages.NotAllowed));
        }

        var entity = await unitOfWork.InvestmentCases.GetScopedAsync(caseId, authResult.Value!, isInternalUser: true, cancellationToken);
        if (entity is null)
        {
            ApplicationLog.Blocked(logger, "RecordValuation", "case not found", authResult.Value, caseId);
            return Result.Fail(Error.NotFound(ApiMessages.CaseNotFound));
        }

        var requiredStatus = request.Type == ValuationType.Primary ? CaseStatus.InitialValuation : CaseStatus.SecondaryValuation;
        if (entity.CurrentStatus != requiredStatus)
        {
            ApplicationLog.Blocked(logger, "RecordValuation",
                $"case status is {entity.CurrentStatus}, expected {requiredStatus}", authResult.Value, caseId);
            return Result.Fail(Error.Conflict(string.Format(ApiMessages.ValuationStatusMismatch, requiredStatus)));
        }

        entity.AddValuation(request.Type, request.Amount, request.Notes, authResult.Value!);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        ApplicationLog.Completed(logger,
            "User {UserId} recorded {ValuationType} valuation {Amount} on case {CaseId} ({CaseNumber})",
            authResult.Value, request.Type, request.Amount, caseId, entity.CaseNumber);

        return Result.Ok();
    }

    public async Task<Result> RecordPaymentAsync(Guid caseId, RecordPaymentRequest request, CancellationToken cancellationToken)
    {
        var authResult = RequireUserId();
        if (authResult.IsFailure) return Result.Fail(authResult.Error!);

        ApplicationLog.Started(logger, "RecordPayment", authResult.Value, caseId);

        if (!authorizationService.HasPermission(CasePermissions.ManagePayments))
        {
            ApplicationLog.Blocked(logger, "RecordPayment", "missing ManagePayments permission", authResult.Value, caseId);
            return Result.Fail(Error.Forbidden(ApiMessages.NotAllowed));
        }

        var entity = await unitOfWork.InvestmentCases.GetScopedAsync(caseId, authResult.Value!, isInternalUser: true, cancellationToken);
        if (entity is null)
        {
            ApplicationLog.Blocked(logger, "RecordPayment", "case not found", authResult.Value, caseId);
            return Result.Fail(Error.NotFound(ApiMessages.CaseNotFound));
        }

        if (entity.CurrentStatus != CaseStatus.WaitingPayment)
        {
            ApplicationLog.Blocked(logger, "RecordPayment", $"case status is {entity.CurrentStatus}", authResult.Value, caseId);
            return Result.Fail(Error.Conflict(ApiMessages.PaymentsOnlyInWaitingPayment));
        }

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

        ApplicationLog.Completed(logger,
            "User {UserId} recorded payment {Amount} ({Status}) on case {CaseId} ({CaseNumber})",
            authResult.Value, request.Amount, request.Status, caseId, entity.CaseNumber);

        return Result.Ok();
    }

    public async Task<Result> UpdatePaymentAsync(Guid caseId, Guid paymentId, UpdatePaymentRequest request, CancellationToken cancellationToken)
    {
        var authResult = RequireUserId();
        if (authResult.IsFailure) return Result.Fail(authResult.Error!);

        ApplicationLog.Started(logger, "UpdatePayment", authResult.Value, caseId);

        if (!authorizationService.HasPermission(CasePermissions.ManagePayments))
        {
            ApplicationLog.Blocked(logger, "UpdatePayment", "missing ManagePayments permission", authResult.Value, caseId);
            return Result.Fail(Error.Forbidden(ApiMessages.NotAllowed));
        }

        var entity = await unitOfWork.InvestmentCases.GetScopedAsync(caseId, authResult.Value!, isInternalUser: true, cancellationToken);
        if (entity is null)
        {
            ApplicationLog.Blocked(logger, "UpdatePayment", "case not found", authResult.Value, caseId);
            return Result.Fail(Error.NotFound(ApiMessages.CaseNotFound));
        }

        var payment = entity.Payments.FirstOrDefault(x => x.Id == paymentId);
        if (payment is null)
        {
            ApplicationLog.Blocked(logger, "UpdatePayment", $"payment {paymentId} not found", authResult.Value, caseId);
            return Result.Fail(Error.NotFound(ApiMessages.PaymentNotFound));
        }

        payment.Update(
            request.Amount,
            request.PaymentDate,
            request.TransactionNumber,
            request.ReceiptS3Key,
            request.Notes,
            request.Method,
            request.Status);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        ApplicationLog.Completed(logger,
            "User {UserId} updated payment {PaymentId} on case {CaseId} ({CaseNumber}) to status {Status}",
            authResult.Value, paymentId, caseId, entity.CaseNumber, request.Status);

        return Result.Ok();
    }

    public async Task<Result> DeletePaymentAsync(Guid caseId, Guid paymentId, CancellationToken cancellationToken)
    {
        var authResult = RequireUserId();
        if (authResult.IsFailure) return Result.Fail(authResult.Error!);

        ApplicationLog.Started(logger, "DeletePayment", authResult.Value, caseId);

        if (!authorizationService.HasPermission(CasePermissions.ManagePayments))
        {
            ApplicationLog.Blocked(logger, "DeletePayment", "missing ManagePayments permission", authResult.Value, caseId);
            return Result.Fail(Error.Forbidden(ApiMessages.NotAllowed));
        }

        var entity = await unitOfWork.InvestmentCases.GetScopedAsync(caseId, authResult.Value!, isInternalUser: true, cancellationToken);
        if (entity is null)
        {
            ApplicationLog.Blocked(logger, "DeletePayment", "case not found", authResult.Value, caseId);
            return Result.Fail(Error.NotFound(ApiMessages.CaseNotFound));
        }

        var payment = entity.Payments.FirstOrDefault(x => x.Id == paymentId);
        if (payment is null)
        {
            ApplicationLog.Blocked(logger, "DeletePayment", $"payment {paymentId} not found", authResult.Value, caseId);
            return Result.Fail(Error.NotFound(ApiMessages.PaymentNotFound));
        }

        entity.Payments.Remove(payment);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        ApplicationLog.Completed(logger,
            "User {UserId} deleted payment {PaymentId} from case {CaseId} ({CaseNumber})",
            authResult.Value, paymentId, caseId, entity.CaseNumber);

        return Result.Ok();
    }

    public async Task<Result<IEnumerable<CaseCommentDto>>> GetCommentsAsync(Guid caseId, bool includeInternal, CancellationToken cancellationToken)
    {
        var authResult = RequireUserId();
        if (authResult.IsFailure) return Result<IEnumerable<CaseCommentDto>>.Fail(authResult.Error!);

        ApplicationLog.Started(logger, "GetComments", authResult.Value, caseId);

        var entity = await unitOfWork.InvestmentCases.GetScopedAsync(caseId, authResult.Value!, authorizationService.IsInternalUser, cancellationToken);
        if (entity is null)
        {
            ApplicationLog.Blocked(logger, "GetComments", "case not found", authResult.Value, caseId);
            return Result<IEnumerable<CaseCommentDto>>.Fail(Error.NotFound(ApiMessages.CaseNotFound));
        }

        var canViewInternal = includeInternal && authorizationService.HasPermission(CasePermissions.ViewInternalComments);
        var isInternalView = authorizationService.IsInternalUser;

        var comments = entity.Comments
            .Where(x => (isInternalView && canViewInternal) || !x.IsInternal)
            .Select(caseDtoMapper.MapComment)
            .OrderBy(x => x.CreatedAt)
            .ToList();

        ApplicationLog.Completed(logger,
            "User {UserId} loaded {Count} comment(s) for case {CaseId} (includeInternal={IncludeInternal})",
            authResult.Value, comments.Count, caseId, includeInternal);

        return Result<IEnumerable<CaseCommentDto>>.Ok(comments);
    }

    public async Task<Result> AddCommentAsync(Guid caseId, AddCommentRequest request, CancellationToken cancellationToken)
    {
        var authResult = RequireUserId();
        if (authResult.IsFailure) return Result.Fail(authResult.Error!);

        ApplicationLog.Started(logger, "AddComment", authResult.Value, caseId);

        var isInternalRequested = request.IsInternal;
        if (isInternalRequested && !authorizationService.HasPermission(CasePermissions.CreateInternalComment))
        {
            ApplicationLog.Blocked(logger, "AddComment", "cannot create internal comment", authResult.Value, caseId);
            return Result.Fail(Error.Forbidden(ApiMessages.NotAllowed));
        }

        var isInternal = isInternalRequested && authorizationService.HasPermission(CasePermissions.CreateInternalComment);

        var entity = await unitOfWork.InvestmentCases.GetScopedAsync(caseId, authResult.Value!, authorizationService.IsInternalUser, cancellationToken);
        if (entity is null)
        {
            ApplicationLog.Blocked(logger, "AddComment", "case not found", authResult.Value, caseId);
            return Result.Fail(Error.NotFound(ApiMessages.CaseNotFound));
        }

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

        ApplicationLog.Completed(logger,
            "User {UserId} added {Visibility} comment on case {CaseId} ({CaseNumber}) in phase {Phase}",
            authResult.Value, isInternal ? "internal" : "public", caseId, entity.CaseNumber, request.Phase);

        return Result.Ok();
    }

    public async Task<Result> AddCommentAttachmentAsync(Guid caseId, Guid commentId, string s3Key, string fileName, CancellationToken cancellationToken)
    {
        var authResult = RequireUserId();
        if (authResult.IsFailure) return Result.Fail(authResult.Error!);

        ApplicationLog.Started(logger, "AddCommentAttachment", authResult.Value, caseId);

        if (!authorizationService.HasPermission(CasePermissions.UploadCommentAttachments))
        {
            ApplicationLog.Blocked(logger, "AddCommentAttachment", "missing UploadCommentAttachments permission", authResult.Value, caseId);
            return Result.Fail(Error.Forbidden(ApiMessages.NotAllowed));
        }

        var entity = await unitOfWork.InvestmentCases.GetScopedAsync(caseId, authResult.Value!, authorizationService.IsInternalUser, cancellationToken);
        if (entity is null)
        {
            ApplicationLog.Blocked(logger, "AddCommentAttachment", "case not found", authResult.Value, caseId);
            return Result.Fail(Error.NotFound(ApiMessages.CaseNotFound));
        }

        var comment = entity.Comments.FirstOrDefault(x => x.Id == commentId);
        if (comment is null)
        {
            ApplicationLog.Blocked(logger, "AddCommentAttachment", $"comment {commentId} not found", authResult.Value, caseId);
            return Result.Fail(Error.NotFound(ApiMessages.CommentNotFound));
        }

        var attachmentValidation = ValidateCommentAttachmentKey(entity.CaseNumber, commentId, s3Key, fileName);
        if (attachmentValidation.IsFailure) return attachmentValidation;

        var exists = await documentStorage.ExistsAsync(s3Key, cancellationToken);
        if (!exists)
        {
            ApplicationLog.Blocked(logger, "AddCommentAttachment", "file not found in storage", authResult.Value, caseId);
            return Result.Fail(Error.Conflict(ApiMessages.UploadedFileNotFound));
        }

        comment.AddAttachment(s3Key, fileName);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        ApplicationLog.Completed(logger,
            "User {UserId} attached file {FileName} to comment {CommentId} on case {CaseId}",
            authResult.Value, fileName, commentId, caseId);

        return Result.Ok();
    }

    public async Task<Result<IEnumerable<CaseDocumentDto>>> GetDocumentsAsync(Guid caseId, CancellationToken cancellationToken)
    {
        var authResult = RequireUserId();
        if (authResult.IsFailure) return Result<IEnumerable<CaseDocumentDto>>.Fail(authResult.Error!);

        ApplicationLog.Started(logger, "GetDocuments", authResult.Value, caseId);

        var entity = await unitOfWork.InvestmentCases.GetScopedAsync(caseId, authResult.Value!, authorizationService.IsInternalUser, cancellationToken);
        if (entity is null)
        {
            ApplicationLog.Blocked(logger, "GetDocuments", "case not found", authResult.Value, caseId);
            return Result<IEnumerable<CaseDocumentDto>>.Fail(Error.NotFound(ApiMessages.CaseNotFound));
        }

        var docs = entity.Documents
            .Select(caseDtoMapper.MapDocument)
            .OrderByDescending(x => x.UploadedAt)
            .ToList();

        ApplicationLog.Completed(logger,
            "User {UserId} loaded {Count} document(s) for case {CaseId} ({CaseNumber})",
            authResult.Value, docs.Count, caseId, entity.CaseNumber);

        return Result<IEnumerable<CaseDocumentDto>>.Ok(docs);
    }

    public async Task<Result<IEnumerable<CaseWorkflowHistoryDto>>> GetHistoryAsync(Guid caseId, CancellationToken cancellationToken)
    {
        var authResult = RequireUserId();
        if (authResult.IsFailure) return Result<IEnumerable<CaseWorkflowHistoryDto>>.Fail(authResult.Error!);

        ApplicationLog.Started(logger, "GetWorkflowHistory", authResult.Value, caseId);

        var entity = await unitOfWork.InvestmentCases.GetScopedAsync(caseId, authResult.Value!, authorizationService.IsInternalUser, cancellationToken);
        if (entity is null)
        {
            ApplicationLog.Blocked(logger, "GetWorkflowHistory", "case not found", authResult.Value, caseId);
            return Result<IEnumerable<CaseWorkflowHistoryDto>>.Fail(Error.NotFound(ApiMessages.CaseNotFound));
        }

        var history = entity.WorkflowHistory
            .Select(caseDtoMapper.MapHistory)
            .OrderBy(x => x.CreatedAt)
            .ToList();

        ApplicationLog.Completed(logger,
            "User {UserId} loaded {Count} workflow history entry(ies) for case {CaseId} ({CaseNumber})",
            authResult.Value, history.Count, caseId, entity.CaseNumber);

        return Result<IEnumerable<CaseWorkflowHistoryDto>>.Ok(history);
    }

    public async Task<Result> UpsertEvaluationAsync(Guid caseId, CaseEvaluationUpsertRequest request, CancellationToken cancellationToken)
    {
        var authResult = RequireUserId();
        if (authResult.IsFailure) return Result.Fail(authResult.Error!);

        ApplicationLog.Started(logger, "UpsertEvaluation", authResult.Value, caseId);

        if (!authorizationService.HasPermission(CasePermissions.UpsertEvaluations))
        {
            ApplicationLog.Blocked(logger, "UpsertEvaluation", "missing UpsertEvaluations permission", authResult.Value, caseId);
            return Result.Fail(Error.Forbidden(ApiMessages.NotAllowed));
        }

        var entity = await unitOfWork.InvestmentCases.GetScopedAsync(caseId, authResult.Value!, isInternalUser: true, cancellationToken);
        if (entity is null)
        {
            ApplicationLog.Blocked(logger, "UpsertEvaluation", "case not found", authResult.Value, caseId);
            return Result.Fail(Error.NotFound(ApiMessages.CaseNotFound));
        }

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

        ApplicationLog.Completed(logger,
            "User {UserId} saved evaluation for phase {Phase} on case {CaseId} ({CaseNumber}) with {ItemCount} item(s)",
            authResult.Value, request.Phase, caseId, entity.CaseNumber, request.Items.Count);

        return Result.Ok();
    }

    public async Task<Result<IEnumerable<CaseEvaluationDto>>> GetEvaluationsAsync(Guid caseId, CancellationToken cancellationToken)
    {
        var authResult = RequireUserId();
        if (authResult.IsFailure) return Result<IEnumerable<CaseEvaluationDto>>.Fail(authResult.Error!);

        ApplicationLog.Started(logger, "GetEvaluations", authResult.Value, caseId);

        if (!authorizationService.HasPermission(CasePermissions.ViewEvaluations))
        {
            ApplicationLog.Blocked(logger, "GetEvaluations", "missing ViewEvaluations permission", authResult.Value, caseId);
            return Result<IEnumerable<CaseEvaluationDto>>.Fail(Error.NotFound(ApiMessages.CaseNotFound));
        }

        var entity = await unitOfWork.InvestmentCases.GetScopedAsync(caseId, authResult.Value!, isInternalUser: true, cancellationToken);
        if (entity is null)
        {
            ApplicationLog.Blocked(logger, "GetEvaluations", "case not found", authResult.Value, caseId);
            return Result<IEnumerable<CaseEvaluationDto>>.Fail(Error.NotFound(ApiMessages.CaseNotFound));
        }

        var evaluations = entity.Evaluations
            .Select(caseDtoMapper.MapEvaluation)
            .OrderByDescending(x => x.CreatedAt)
            .ToList();

        ApplicationLog.Completed(logger,
            "User {UserId} loaded {Count} evaluation(s) for case {CaseId}",
            authResult.Value, evaluations.Count, caseId);

        return Result<IEnumerable<CaseEvaluationDto>>.Ok(evaluations);
    }

    public async Task<Result<IEnumerable<InvestmentCaseDto>>> SearchAsync(CaseSearchRequest request, CancellationToken cancellationToken)
    {
        var authResult = RequireUserId();
        if (authResult.IsFailure) return Result<IEnumerable<InvestmentCaseDto>>.Fail(authResult.Error!);

        ApplicationLog.Started(logger, "SearchInvestmentCases", authResult.Value);

        var results = await unitOfWork.InvestmentCases.SearchScopedAsync(
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

        var resultList = results.ToList();
        var dtos = resultList.Select(x => caseDtoMapper.MapCase(x, clock.UtcNow, authorizationService.IsInternalUser));

        ApplicationLog.Completed(logger,
            "User {UserId} searched investment cases — page {Page}, size {PageSize}, returned {Count} result(s)",
            authResult.Value, request.Page, request.PageSize, resultList.Count);

        return Result<IEnumerable<InvestmentCaseDto>>.Ok(dtos);
    }

    public async Task<Result<PresignUploadResponse>> PresignDocumentUploadAsync(Guid caseId, PresignUploadRequest request, CancellationToken cancellationToken)
    {
        var authResult = RequireUserId();
        if (authResult.IsFailure) return Result<PresignUploadResponse>.Fail(authResult.Error!);

        ApplicationLog.Started(logger, "PresignDocumentUpload", authResult.Value, caseId);

        var entity = await unitOfWork.InvestmentCases.GetScopedWithDocumentsAsync(caseId, authResult.Value!, authorizationService.IsInternalUser, cancellationToken);
        if (entity is null)
        {
            ApplicationLog.Blocked(logger, "PresignDocumentUpload", "case not found", authResult.Value, caseId);
            return Result<PresignUploadResponse>.Fail(Error.NotFound(ApiMessages.CaseNotFound));
        }

        var validation = ValidatePresignRequest(entity, request);
        if (validation.IsFailure) return Result<PresignUploadResponse>.Fail(validation.Error!);

        var version = entity.Documents.Where(x => x.DocumentType == request.DocumentType).Select(x => x.Version).DefaultIfEmpty(0).Max() + 1;
        var ext = GetSafeExtension(request.FileName);
        var s3Key = $"cases/{entity.CaseNumber}/{request.DocumentType}/{version}{ext}";

        var (url, expiresAt) = await documentStorage.PresignUploadAsync(s3Key, request.MimeType, TimeSpan.FromMinutes(15), cancellationToken);

        ApplicationLog.Completed(logger,
            "User {UserId} received presigned upload URL for {DocumentType} v{Version} on case {CaseId} ({CaseNumber})",
            authResult.Value, request.DocumentType, version, caseId, entity.CaseNumber);

        return Result<PresignUploadResponse>.Ok(new PresignUploadResponse(s3Key, url, expiresAt, version));
    }

    public async Task<Result<CaseDocumentDto>> UploadDocumentAsync(
        Guid caseId,
        PresignUploadRequest request,
        Stream content,
        CancellationToken cancellationToken)
    {
        var authResult = RequireUserId();
        if (authResult.IsFailure) return Result<CaseDocumentDto>.Fail(authResult.Error!);

        ApplicationLog.Started(logger, "UploadDocument", authResult.Value, caseId);

        var entity = await unitOfWork.InvestmentCases.GetScopedWithDocumentsAsync(caseId, authResult.Value!, authorizationService.IsInternalUser, cancellationToken);
        if (entity is null)
        {
            ApplicationLog.Blocked(logger, "UploadDocument", "case not found", authResult.Value, caseId);
            return Result<CaseDocumentDto>.Fail(Error.NotFound(ApiMessages.CaseNotFound));
        }

        var normalized = NormalizePresignRequest(request);
        var keyResult = TryBuildDocumentUploadKey(entity, normalized);
        if (keyResult.IsFailure) return Result<CaseDocumentDto>.Fail(keyResult.Error!);

        var (s3Key, version) = keyResult.Value!;
        if (entity.Documents.Any(x => string.Equals(x.S3Key, s3Key, StringComparison.Ordinal)))
        {
            ApplicationLog.Blocked(logger, "UploadDocument", "document already exists", authResult.Value, caseId);
            return Result<CaseDocumentDto>.Fail(Error.Conflict(ApiMessages.DocumentAlreadyExists));
        }

        var authorization = EnsureCanConfirmDocument(entity, normalized.DocumentType);
        if (authorization.IsFailure) return Result<CaseDocumentDto>.Fail(authorization.Error!);

        await documentStorage.UploadAsync(s3Key, content, normalized.MimeType, cancellationToken);

        var document = entity.AddDocument(
            s3Key,
            Path.GetFileName(normalized.FileName),
            normalized.MimeType,
            normalized.FileSize,
            version,
            normalized.DocumentType,
            authResult.Value!);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        ApplicationLog.Completed(logger,
            "User {UserId} uploaded {DocumentType} v{Version} ({FileName}) to case {CaseId} ({CaseNumber})",
            authResult.Value, normalized.DocumentType, version, normalized.FileName, caseId, entity.CaseNumber);

        return Result<CaseDocumentDto>.Ok(caseDtoMapper.MapDocument(document));
    }

    public async Task<Result<CaseDocumentDto>> ConfirmDocumentUploadedAsync(Guid caseId, string s3Key, CancellationToken cancellationToken)
    {
        var authResult = RequireUserId();
        if (authResult.IsFailure) return Result<CaseDocumentDto>.Fail(authResult.Error!);

        ApplicationLog.Started(logger, "ConfirmDocumentUpload", authResult.Value, caseId);

        var entity = await unitOfWork.InvestmentCases.GetScopedWithDocumentsAsync(caseId, authResult.Value!, authorizationService.IsInternalUser, cancellationToken);
        if (entity is null)
        {
            ApplicationLog.Blocked(logger, "ConfirmDocumentUpload", "case not found", authResult.Value, caseId);
            return Result<CaseDocumentDto>.Fail(Error.NotFound(ApiMessages.CaseNotFound));
        }

        var keyValidation = ValidateAndNormalizeDocumentKey(entity.CaseNumber, s3Key);
        if (keyValidation.IsFailure) return Result<CaseDocumentDto>.Fail(keyValidation.Error!);

        s3Key = keyValidation.Value!;

        if (entity.Documents.Any(x => string.Equals(x.S3Key, s3Key, StringComparison.Ordinal)))
        {
            ApplicationLog.Blocked(logger, "ConfirmDocumentUpload", "document already registered", authResult.Value, caseId);
            return Result<CaseDocumentDto>.Fail(Error.Conflict(ApiMessages.DocumentAlreadyExists));
        }

        var docType = ExtractDocumentTypeFromKey(s3Key);
        var authorization = EnsureCanConfirmDocument(entity, docType);
        if (authorization.IsFailure) return Result<CaseDocumentDto>.Fail(authorization.Error!);

        var exists = await documentStorage.ExistsAsync(s3Key, cancellationToken);
        if (!exists)
        {
            ApplicationLog.Blocked(logger, "ConfirmDocumentUpload", "file not found in storage", authResult.Value, caseId);
            return Result<CaseDocumentDto>.Fail(Error.Conflict(ApiMessages.UploadedFileNotFound));
        }

        var ext = Path.GetExtension(s3Key);
        var expectedVersion = entity.Documents.Where(x => x.DocumentType == docType).Select(x => x.Version).DefaultIfEmpty(0).Max() + 1;
        var expectedKey = $"cases/{entity.CaseNumber}/{docType}/{expectedVersion}{ext}";
        if (!string.Equals(s3Key, expectedKey, StringComparison.Ordinal))
        {
            ApplicationLog.Blocked(logger, "ConfirmDocumentUpload", "S3 key does not match expected version", authResult.Value, caseId);
            return Result<CaseDocumentDto>.Fail(Error.Forbidden(ApiMessages.NotAllowed));
        }

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

        ApplicationLog.Completed(logger,
            "User {UserId} confirmed {DocumentType} v{Version} on case {CaseId} ({CaseNumber})",
            authResult.Value, docType, version, caseId, entity.CaseNumber);

        return Result<CaseDocumentDto>.Ok(caseDtoMapper.MapDocument(document));
    }

    public async Task<Result<PresignDownloadResponse>> PresignDocumentDownloadAsync(Guid caseId, Guid documentId, CancellationToken cancellationToken)
    {
        var authResult = RequireUserId();
        if (authResult.IsFailure) return Result<PresignDownloadResponse>.Fail(authResult.Error!);

        ApplicationLog.Started(logger, "PresignDocumentDownload", authResult.Value, caseId);

        if (!authorizationService.HasPermission(CasePermissions.DownloadDocuments))
        {
            ApplicationLog.Blocked(logger, "PresignDocumentDownload", "missing DownloadDocuments permission", authResult.Value, caseId);
            return Result<PresignDownloadResponse>.Fail(Error.Forbidden(ApiMessages.NotAllowed));
        }

        var entity = await unitOfWork.InvestmentCases.GetScopedWithDocumentsAsync(caseId, authResult.Value!, authorizationService.IsInternalUser, cancellationToken);
        if (entity is null)
        {
            ApplicationLog.Blocked(logger, "PresignDocumentDownload", "case not found", authResult.Value, caseId);
            return Result<PresignDownloadResponse>.Fail(Error.NotFound(ApiMessages.CaseNotFound));
        }

        var document = entity.Documents.FirstOrDefault(x => x.Id == documentId);
        if (document is null)
        {
            ApplicationLog.Blocked(logger, "PresignDocumentDownload", $"document {documentId} not found", authResult.Value, caseId);
            return Result<PresignDownloadResponse>.Fail(Error.NotFound(ApiMessages.DocumentNotFound));
        }

        var (url, expiresAt) = await documentStorage.PresignDownloadAsync(document.S3Key, TimeSpan.FromMinutes(10), cancellationToken);

        ApplicationLog.Completed(logger,
            "User {UserId} received presigned download URL for document {DocumentId} ({FileName}) on case {CaseId}",
            authResult.Value, documentId, document.FileName, caseId);

        return Result<PresignDownloadResponse>.Ok(new PresignDownloadResponse(url, expiresAt, document.FileName));
    }

    private Result<string> RequireUserId()
    {
        var auth = authorizationService.EnsureAuthenticated();
        if (auth.IsFailure) return Result<string>.Fail(auth.Error!);
        return Result<string>.Ok(authorizationService.UserId!);
    }

    private static PresignUploadRequest NormalizePresignRequest(PresignUploadRequest request)
    {
        var mimeType = NormalizeMimeType(request.FileName, request.MimeType);
        return request with { MimeType = mimeType };
    }

    private Result<(string S3Key, int Version)> TryBuildDocumentUploadKey(InvestmentCase entity, PresignUploadRequest request)
    {
        var validation = ValidatePresignRequest(entity, request);
        if (validation.IsFailure) return Result<(string, int)>.Fail(validation.Error!);

        var version = entity.Documents.Where(x => x.DocumentType == request.DocumentType).Select(x => x.Version).DefaultIfEmpty(0).Max() + 1;
        var ext = GetSafeExtension(request.FileName);
        var s3Key = $"cases/{entity.CaseNumber}/{request.DocumentType}/{version}{ext}";
        return Result<(string, int)>.Ok((s3Key, version));
    }

    private Result ValidatePresignRequest(InvestmentCase entity, PresignUploadRequest request)
    {
        if (!IsSafeClientFileName(request.FileName))
            return Result.Fail(Error.Validation(ApiMessages.InvalidFileName));

        var ext = GetSafeExtension(request.FileName);
        if (!IsAllowedUpload(request.MimeType, ext))
            return Result.Fail(Error.Validation(ApiMessages.FileTypeNotAllowed));

        if (authorizationService.IsInternalUser)
        {
            if (request.DocumentType is DocumentType.PreContract or DocumentType.FinalContract or DocumentType.SignedContract)
            {
                if (!authorizationService.HasPermission(CasePermissions.ManageContracts))
                    return Result.Fail(Error.Forbidden(ApiMessages.NotAllowed));

                if (request.DocumentType is DocumentType.PreContract && entity.CurrentStatus != CaseStatus.WaitingPreliminaryContract)
                    return Result.Fail(Error.Conflict(ApiMessages.DocumentUploadNotAllowed));

                if (request.DocumentType is DocumentType.FinalContract && entity.CurrentStatus is not (CaseStatus.ContractDrafting or CaseStatus.WaitingContractSignature))
                    return Result.Fail(Error.Conflict(ApiMessages.DocumentUploadNotAllowed));

                if (request.DocumentType is DocumentType.SignedContract && entity.CurrentStatus != CaseStatus.WaitingSignedContractUpload)
                    return Result.Fail(Error.Conflict(ApiMessages.DocumentUploadNotAllowed));

                return Result.Ok();
            }

            if (request.DocumentType is DocumentType.PaymentReceipt)
            {
                if (!authorizationService.HasPermission(CasePermissions.ManagePayments))
                    return Result.Fail(Error.Forbidden(ApiMessages.NotAllowed));

                if (entity.CurrentStatus != CaseStatus.WaitingPayment)
                    return Result.Fail(Error.Conflict(ApiMessages.DocumentUploadNotAllowed));

                return Result.Ok();
            }

            if (!authorizationService.HasPermission(CasePermissions.UploadDocuments))
                return Result.Fail(Error.Forbidden(ApiMessages.NotAllowed));

            return Result.Ok();
        }

        if (!authorizationService.HasPermission(CasePermissions.UploadDocuments))
            return Result.Fail(Error.Forbidden(ApiMessages.NotAllowed));

        if (request.DocumentType is DocumentType.PreContract or DocumentType.FinalContract or DocumentType.SignedContract or DocumentType.PaymentReceipt)
            return Result.Fail(Error.Forbidden(ApiMessages.NotAllowed));

        if (entity.CurrentStatus is not (CaseStatus.Draft or CaseStatus.DataEntry1 or CaseStatus.DataEntry2))
            return Result.Fail(Error.Conflict(ApiMessages.DocumentUploadNotAllowed));

        return Result.Ok();
    }

    private Result EnsureCanConfirmDocument(InvestmentCase entity, DocumentType documentType)
    {
        if (!authorizationService.IsInternalUser)
        {
            if (documentType is DocumentType.PreContract or DocumentType.FinalContract or DocumentType.SignedContract or DocumentType.PaymentReceipt)
                return Result.Fail(Error.Forbidden(ApiMessages.NotAllowed));

            if (entity.CurrentStatus is not (CaseStatus.Draft or CaseStatus.DataEntry1 or CaseStatus.DataEntry2))
                return Result.Fail(Error.Conflict(ApiMessages.DocumentConfirmationNotAllowed));

            return Result.Ok();
        }

        if (documentType is DocumentType.PreContract or DocumentType.FinalContract or DocumentType.SignedContract)
        {
            if (!authorizationService.HasPermission(CasePermissions.ManageContracts))
                return Result.Fail(Error.Forbidden(ApiMessages.NotAllowed));

            if (documentType is DocumentType.PreContract && entity.CurrentStatus != CaseStatus.WaitingPreliminaryContract)
                return Result.Fail(Error.Conflict(ApiMessages.DocumentConfirmationNotAllowed));

            if (documentType is DocumentType.FinalContract && entity.CurrentStatus is not (CaseStatus.ContractDrafting or CaseStatus.WaitingContractSignature))
                return Result.Fail(Error.Conflict(ApiMessages.DocumentConfirmationNotAllowed));

            if (documentType is DocumentType.SignedContract && entity.CurrentStatus != CaseStatus.WaitingSignedContractUpload)
                return Result.Fail(Error.Conflict(ApiMessages.DocumentConfirmationNotAllowed));

            return Result.Ok();
        }

        if (documentType is DocumentType.PaymentReceipt)
        {
            if (!authorizationService.HasPermission(CasePermissions.ManagePayments))
                return Result.Fail(Error.Forbidden(ApiMessages.NotAllowed));

            if (entity.CurrentStatus != CaseStatus.WaitingPayment)
                return Result.Fail(Error.Conflict(ApiMessages.DocumentConfirmationNotAllowed));

            return Result.Ok();
        }

        return authorizationService.HasPermission(CasePermissions.UploadDocuments) ? Result.Ok() : Result.Fail(Error.Forbidden(ApiMessages.NotAllowed));
    }

    private static Result<string> ValidateAndNormalizeDocumentKey(string caseNumber, string s3Key)
    {
        if (string.IsNullOrWhiteSpace(s3Key))
            return Result<string>.Fail(Error.Validation(ApiMessages.InvalidDocumentKey));

        if (s3Key.Contains('\\') || s3Key.Contains("..", StringComparison.Ordinal))
            return Result<string>.Fail(Error.Validation(ApiMessages.InvalidDocumentKey));

        if (!s3Key.StartsWith($"cases/{caseNumber}/", StringComparison.Ordinal))
            return Result<string>.Fail(Error.Forbidden(ApiMessages.NotAllowed));

        var parts = s3Key.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 4)
            return Result<string>.Fail(Error.Validation(ApiMessages.InvalidDocumentKey));

        if (!Enum.TryParse<DocumentType>(parts[2], ignoreCase: true, out _))
            return Result<string>.Fail(Error.Validation(ApiMessages.InvalidDocumentKey));

        var file = parts[3];
        if (file.Length > 128)
            return Result<string>.Fail(Error.Validation(ApiMessages.InvalidDocumentKey));

        var ext = Path.GetExtension(file);
        if (string.IsNullOrWhiteSpace(ext) || ext.Length > 10)
            return Result<string>.Fail(Error.Validation(ApiMessages.InvalidDocumentKey));

        var fileNameWithoutExt = Path.GetFileNameWithoutExtension(file);
        if (!int.TryParse(fileNameWithoutExt, out _))
            return Result<string>.Fail(Error.Validation(ApiMessages.InvalidDocumentKey));

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

    private static string NormalizeMimeType(string fileName, string mimeType)
    {
        return GetSafeExtension(fileName) switch
        {
            ".pdf" => "application/pdf",
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            _ => string.IsNullOrWhiteSpace(mimeType) ? "application/octet-stream" : mimeType.Trim()
        };
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
            return Result.Fail(Error.Validation(ApiMessages.InvalidAttachmentKey));

        if (!IsSafeClientFileName(fileName))
            return Result.Fail(Error.Validation(ApiMessages.InvalidFileName));

        if (s3Key.Contains('\\') || s3Key.Contains("..", StringComparison.Ordinal))
            return Result.Fail(Error.Validation(ApiMessages.InvalidAttachmentKey));

        var prefix = $"cases/{caseNumber}/comments/{commentId:D}/";
        if (!s3Key.StartsWith(prefix, StringComparison.Ordinal))
            return Result.Fail(Error.Forbidden(ApiMessages.NotAllowed));

        return Result.Ok();
    }
}