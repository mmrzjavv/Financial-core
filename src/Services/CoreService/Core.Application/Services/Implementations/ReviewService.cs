using BuildingBlocks.Application.Validation;
using Core.Application.Common;
using BuildingBlocks.Application.Abstractions;
using BuildingBlocks.Application.Errors;
using BuildingBlocks.Application.Results;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Services.CoreService.Core.Application.Abstractions;
using Services.CoreService.Core.Application.Contracts.Reviews;
using Services.CoreService.Core.Domain.Constants;
using Services.CoreService.Core.Domain.Enums;



namespace Services.CoreService.Core.Application.Services.Implementations;

public sealed class ReviewService : IReviewService
{
    private readonly ICoreDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly ICaseStateManager _stateManager;
    private readonly ICaseWorkflowOrchestrator _workflowOrchestrator;
    private readonly IValidator<ApprovePhaseRequest> _approveValidator;
    private readonly IValidator<RequestRevisionForPhaseRequest> _revisionValidator;

    public ReviewService(
        ICoreDbContext db,
        ICurrentUser currentUser,
        ICaseStateManager stateManager,
        ICaseWorkflowOrchestrator workflowOrchestrator,
        IValidator<ApprovePhaseRequest> approveValidator,
        IValidator<RequestRevisionForPhaseRequest> revisionValidator)
    {
        _db = db;
        _currentUser = currentUser;
        _stateManager = stateManager;
        _workflowOrchestrator = workflowOrchestrator;
        _approveValidator = approveValidator;
        _revisionValidator = revisionValidator;
    }

    public async Task<Result> ApproveAsync(Guid caseId, ApprovePhaseRequest request, CancellationToken ct)
    {
        var validation = await _approveValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
            return Result.Fail(Error.Validation(validation.ToErrorMessage()));

        if (_currentUser.Roles.Contains(UserRoleClaims.Applicant))
            return Result.Fail(Error.Forbidden());

        var entity = await _db.InvestmentCases
            .Include(x => x.WorkflowHistory)
            .FirstOrDefaultAsync(x => x.Id == caseId, ct);

        if (entity is null)
            return Result.Fail(Error.NotFound(ApiMessages.CaseNotFound));

        if (entity.CurrentPhase != request.Phase)
            return Result.Fail(Error.Conflict(ApiMessages.ApprovalMustMatchPhase));

        var (nextPhase, nextStatus) = GetNext(request.Phase);
        var transition = _stateManager.ValidateTransition(entity.CurrentPhase, entity.CurrentStatus, nextPhase, nextStatus);
        if (transition.IsFailure)
            return transition;

        entity.ChangePhase(nextPhase, nextStatus, _currentUser.UserId, request.Comment);
        await _workflowOrchestrator.SignalAsync(caseId, WorkflowSignals.StatusChanged, new { caseId, phase = request.Phase }, ct);
        await _db.SaveChangesAsync(ct);
        return Result.Ok();
    }

    public async Task<Result> RequestRevisionAsync(Guid caseId, RequestRevisionForPhaseRequest request, CancellationToken ct)
    {
        var validation = await _revisionValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
            return Result.Fail(Error.Validation(validation.ToErrorMessage()));

        if (_currentUser.Roles.Contains(UserRoleClaims.Applicant))
            return Result.Fail(Error.Forbidden());

        var entity = await _db.InvestmentCases
            .Include(x => x.Comments)
            .FirstOrDefaultAsync(x => x.Id == caseId, ct);

        if (entity is null)
            return Result.Fail(Error.NotFound(ApiMessages.CaseNotFound));

        if (entity.CurrentPhase != request.Phase)
            return Result.Fail(Error.Conflict(ApiMessages.RevisionMustMatchPhase));

        entity.RequestRevision(request.Phase, _currentUser.UserId, request.Message, request.IsInternal);
        await _workflowOrchestrator.SignalAsync(caseId, WorkflowSignals.StatusChanged, new { caseId, request.Phase }, ct);

        await _db.SaveChangesAsync(ct);
        return Result.Ok();
    }

    private static (CasePhase Phase, CaseStatus Status) GetNext(CasePhase phase) =>
        phase switch
        {
            CasePhase.DataEntry1 => (CasePhase.ExpertReview1, CaseStatus.WaitingForReview),
            CasePhase.ExpertReview1 => (CasePhase.DataEntry2, CaseStatus.Draft),
            CasePhase.DataEntry2 => (CasePhase.ExpertReview2, CaseStatus.WaitingForReview),
            CasePhase.ExpertReview2 => (CasePhase.InitialValuation, CaseStatus.WaitingForReview),
            CasePhase.InitialValuation => (CasePhase.InitialValuation, CaseStatus.Approved),
            CasePhase.SecondaryValuation => (CasePhase.SecondaryValuation, CaseStatus.Approved),
            CasePhase.PreliminaryContract => (CasePhase.UserContractReview, CaseStatus.WaitingForUser),
            CasePhase.UserContractReview => (CasePhase.ContractDrafting, CaseStatus.InProgress),
            CasePhase.ContractDrafting => (CasePhase.WaitingForSignature, CaseStatus.InProgress),
            CasePhase.WaitingForSignature => (CasePhase.ContractUpload, CaseStatus.InProgress),
            CasePhase.ContractUpload => (CasePhase.InvestmentCaseFinancialWorksheet, CaseStatus.Draft),
            CasePhase.InvestmentCaseFinancialWorksheet => (CasePhase.FinanceReview, CaseStatus.WaitingForFinance),
            CasePhase.FinanceReview => (CasePhase.Payments, CaseStatus.InProgress),
            CasePhase.Payments => (CasePhase.Completion, CaseStatus.Completed),
            _ => (phase, CaseStatus.InProgress)
        };
}