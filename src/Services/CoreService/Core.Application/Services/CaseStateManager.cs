using Core.Application.Common;
using BuildingBlocks.Application.Errors;
using BuildingBlocks.Application.Results;
using Core.Application.Abstractions;
using Services.CoreService.Core.Domain.Constants;
using Services.CoreService.Core.Domain.Entities;
using Services.CoreService.Core.Domain.Enums;


namespace Core.Application.Services;

public sealed class CaseStateManager : ICaseStateManager
{
    private static readonly HashSet<CaseStatus> TerminalStates =
    [
        CaseStatus.Rejected,
        CaseStatus.Cancelled,
        CaseStatus.Archived,
        CaseStatus.Completed
    ];

    private static readonly Dictionary<(CaseStatus Current, WorkflowAction Action, string Role), CaseStatus> Transitions = new()
    {
        { (CaseStatus.Draft, WorkflowAction.Submit, SystemRoles.Applicant), CaseStatus.DataEntry1 },
        { (CaseStatus.Draft, WorkflowAction.Cancel, SystemRoles.Applicant), CaseStatus.Cancelled },

        { (CaseStatus.DataEntry1, WorkflowAction.Submit, SystemRoles.Applicant), CaseStatus.ReviewDataEntry1 },
        { (CaseStatus.DataEntry1, WorkflowAction.Cancel, SystemRoles.Applicant), CaseStatus.Cancelled },

        { (CaseStatus.ReviewDataEntry1, WorkflowAction.Approve, SystemRoles.InvestmentExpert), CaseStatus.DataEntry2 },
        { (CaseStatus.ReviewDataEntry1, WorkflowAction.RequestRevision, SystemRoles.InvestmentExpert), CaseStatus.DataEntry1 },
        { (CaseStatus.ReviewDataEntry1, WorkflowAction.Reject, SystemRoles.InvestmentExpert), CaseStatus.Rejected },

        { (CaseStatus.DataEntry2, WorkflowAction.Submit, SystemRoles.Applicant), CaseStatus.ReviewDataEntry2 },
        { (CaseStatus.DataEntry2, WorkflowAction.Cancel, SystemRoles.Applicant), CaseStatus.Cancelled },

        { (CaseStatus.ReviewDataEntry2, WorkflowAction.Approve, SystemRoles.InvestmentExpert), CaseStatus.InitialValuation },
        { (CaseStatus.ReviewDataEntry2, WorkflowAction.RequestRevision, SystemRoles.InvestmentExpert), CaseStatus.DataEntry2 },
        { (CaseStatus.ReviewDataEntry2, WorkflowAction.Reject, SystemRoles.InvestmentExpert), CaseStatus.Rejected },

        { (CaseStatus.InitialValuation, WorkflowAction.Approve, SystemRoles.InvestmentManager), CaseStatus.SecondaryValuation },
        { (CaseStatus.InitialValuation, WorkflowAction.Reject, SystemRoles.InvestmentManager), CaseStatus.Rejected },

        { (CaseStatus.SecondaryValuation, WorkflowAction.Approve, SystemRoles.InvestmentManager), CaseStatus.WaitingPreliminaryContract },
        { (CaseStatus.SecondaryValuation, WorkflowAction.Reject, SystemRoles.InvestmentManager), CaseStatus.Rejected },

        { (CaseStatus.WaitingPreliminaryContract, WorkflowAction.UploadPreliminaryContract, SystemRoles.LegalExpert), CaseStatus.WaitingUserReviewPreliminaryContract },
        { (CaseStatus.WaitingPreliminaryContract, WorkflowAction.Reject, SystemRoles.LegalExpert), CaseStatus.Rejected },

        { (CaseStatus.WaitingUserReviewPreliminaryContract, WorkflowAction.Approve, SystemRoles.Applicant), CaseStatus.ContractDrafting },
        { (CaseStatus.WaitingUserReviewPreliminaryContract, WorkflowAction.RequestRevision, SystemRoles.Applicant), CaseStatus.WaitingPreliminaryContract },
        { (CaseStatus.WaitingUserReviewPreliminaryContract, WorkflowAction.Cancel, SystemRoles.Applicant), CaseStatus.Cancelled },

        { (CaseStatus.ContractDrafting, WorkflowAction.FinalizeContractDraft, SystemRoles.LegalExpert), CaseStatus.WaitingContractSignature },
        { (CaseStatus.ContractDrafting, WorkflowAction.Reject, SystemRoles.LegalExpert), CaseStatus.Rejected },

        { (CaseStatus.WaitingContractSignature, WorkflowAction.ConfirmSignature, SystemRoles.LegalExpert), CaseStatus.WaitingSignedContractUpload },
        { (CaseStatus.WaitingContractSignature, WorkflowAction.Reject, SystemRoles.LegalExpert), CaseStatus.Rejected },

        { (CaseStatus.WaitingSignedContractUpload, WorkflowAction.UploadSignedContract, SystemRoles.LegalExpert), CaseStatus.WaitingFinancialWorksheet },
        { (CaseStatus.WaitingSignedContractUpload, WorkflowAction.Reject, SystemRoles.LegalExpert), CaseStatus.Rejected },

        { (CaseStatus.WaitingFinancialWorksheet, WorkflowAction.SubmitFinancialWorksheet, SystemRoles.InvestmentExpert), CaseStatus.FinancialWorksheetReview },
        { (CaseStatus.WaitingFinancialWorksheet, WorkflowAction.Reject, SystemRoles.InvestmentExpert), CaseStatus.Rejected },

        { (CaseStatus.FinancialWorksheetReview, WorkflowAction.ApproveFinancialWorksheet, SystemRoles.FinancialExpert), CaseStatus.WaitingPayment },
        { (CaseStatus.FinancialWorksheetReview, WorkflowAction.RequestRevision, SystemRoles.FinancialExpert), CaseStatus.WaitingFinancialWorksheet },
        { (CaseStatus.FinancialWorksheetReview, WorkflowAction.Reject, SystemRoles.FinancialExpert), CaseStatus.Rejected },

        { (CaseStatus.WaitingPayment, WorkflowAction.CompletePayment, SystemRoles.FinancialExpert), CaseStatus.Completed },
        { (CaseStatus.WaitingPayment, WorkflowAction.Cancel, SystemRoles.FinancialExpert), CaseStatus.Cancelled }
    };

    public bool IsTerminalState(CaseStatus status) => TerminalStates.Contains(status);

    public bool CanTransition(
        CaseStatus currentStatus,
        WorkflowAction action,
        string userRole,
        out CaseStatus nextStatus,
        out string errorMessage)
    {
        nextStatus = currentStatus;

        if (IsTerminalState(currentStatus))
        {
            errorMessage = ApiMessages.CannotTransitionFromTerminalState;
            return false;
        }

        if (action == WorkflowAction.Archive)
        {
            if (!string.Equals(userRole, SystemRoles.Admin, StringComparison.OrdinalIgnoreCase))
            {
                errorMessage = ApiMessages.OnlyAdminCanArchive;
                return false;
            }

            nextStatus = CaseStatus.Archived;
            errorMessage = string.Empty;
            return true;
        }

        if (Transitions.TryGetValue((currentStatus, action, userRole), out var directNext))
        {
            nextStatus = directNext;
            errorMessage = string.Empty;
            return true;
        }

        if (string.Equals(userRole, SystemRoles.Admin, StringComparison.OrdinalIgnoreCase))
        {
            var candidates = Transitions
                .Where(x => x.Key.Current == currentStatus && x.Key.Action == action)
                .Select(x => x.Value)
                .Distinct()
                .ToArray();

            if (candidates.Length == 1)
            {
                nextStatus = candidates[0];
                errorMessage = string.Empty;
                return true;
            }
        }

        errorMessage = ApiMessages.InvalidTransition;
        return false;
    }

    public Task<Result> TransitionAsync(
        InvestmentCase caseEntity,
        WorkflowAction action,
        string actorId,
        string actorRole,
        string? comment = null,
        Guid? correlationId = null)
    {
        if (caseEntity is null)
            return Task.FromResult(Result.Fail(Error.Unexpected(ApiMessages.CaseEntityIsNull)));

        correlationId ??= Guid.NewGuid();
        if (caseEntity.WorkflowHistory.Any(x => x.CorrelationId == correlationId.Value))
            return Task.FromResult(Result.Ok());

        if (action == WorkflowAction.Archive && caseEntity.CurrentStatus == CaseStatus.Archived)
            return Task.FromResult(Result.Ok());

        if (!CanTransition(caseEntity.CurrentStatus, action, actorRole, out var nextStatus, out var errorMessage))
        {
            if (string.Equals(errorMessage, ApiMessages.OnlyAdminCanArchive, StringComparison.Ordinal))
                return Task.FromResult(Result.Fail(Error.Forbidden(errorMessage)));

            return Task.FromResult(Result.Fail(Error.Conflict(errorMessage)));
        }

        if (nextStatus == caseEntity.CurrentStatus)
            return Task.FromResult(Result.Ok());

        if (!ValidateBusinessRules(caseEntity, action, nextStatus, out var businessError))
            return Task.FromResult(Result.Fail(Error.Conflict(businessError)));

        if (action == WorkflowAction.RequestRevision)
        {
            caseEntity.RequestRevision(nextStatus, actorId, actorRole, action, correlationId.Value, comment ?? string.Empty, isInternal: false);
            return Task.FromResult(Result.Ok());
        }

        caseEntity.TransitionTo(nextStatus, actorId, actorRole, action, correlationId.Value, comment);
        return Task.FromResult(Result.Ok());
    }

    private static bool ValidateBusinessRules(
        InvestmentCase caseEntity,
        WorkflowAction action,
        CaseStatus nextStatus,
        out string errorMessage)
    {
        switch (action)
        {
            case WorkflowAction.Submit when caseEntity.CurrentStatus == CaseStatus.DataEntry1:
                if (caseEntity.DataEntry1 is null)
                {
                    errorMessage = ApiMessages.CannotSubmitDataEntry1BeforeSave;
                    return false;
                }
                if (string.IsNullOrWhiteSpace(caseEntity.DataEntry1.StartupTitle) ||
                    string.IsNullOrWhiteSpace(caseEntity.DataEntry1.BusinessDescription) ||
                    caseEntity.DataEntry1.RequestedAmount <= 0 ||
                    caseEntity.DataEntry1.TeamSize <= 0)
                {
                    errorMessage = ApiMessages.DataEntry1Incomplete;
                    return false;
                }
                break;

            case WorkflowAction.Submit when caseEntity.CurrentStatus == CaseStatus.DataEntry2:
                if (caseEntity.DataEntry2 is null)
                {
                    errorMessage = ApiMessages.CannotSubmitDataEntry2BeforeSave;
                    return false;
                }
                if (string.IsNullOrWhiteSpace(caseEntity.DataEntry2.MarketAnalysis) ||
                    string.IsNullOrWhiteSpace(caseEntity.DataEntry2.RevenueModel) ||
                    string.IsNullOrWhiteSpace(caseEntity.DataEntry2.CompetitiveAdvantage) ||
                    string.IsNullOrWhiteSpace(caseEntity.DataEntry2.FinancialProjection))
                {
                    errorMessage = ApiMessages.DataEntry2Incomplete;
                    return false;
                }
                break;

            case WorkflowAction.UploadPreliminaryContract when nextStatus == CaseStatus.WaitingUserReviewPreliminaryContract:
                if (!caseEntity.Documents.Any(x => x.DocumentType == DocumentType.PreContract))
                {
                    errorMessage = ApiMessages.PreliminaryContractMissing;
                    return false;
                }
                break;

            case WorkflowAction.UploadSignedContract when nextStatus == CaseStatus.WaitingFinancialWorksheet:
                if (!caseEntity.Documents.Any(x => x.DocumentType == DocumentType.SignedContract))
                {
                    errorMessage = ApiMessages.SignedContractMissing;
                    return false;
                }
                break;

            case WorkflowAction.SubmitFinancialWorksheet when nextStatus == CaseStatus.FinancialWorksheetReview:
            case WorkflowAction.ApproveFinancialWorksheet when nextStatus == CaseStatus.WaitingPayment:
                if (caseEntity.FinancialWorksheet is null || caseEntity.FinancialWorksheet.ApprovedAmount <= 0)
                {
                    errorMessage = ApiMessages.FinancialWorksheetMissingOrInvalid;
                    return false;
                }
                break;

            case WorkflowAction.CompletePayment when nextStatus == CaseStatus.Completed:
                if (caseEntity.FinancialWorksheet is null || caseEntity.FinancialWorksheet.ApprovedAmount <= 0)
                {
                    errorMessage = ApiMessages.ApprovedAmountNotSet;
                    return false;
                }
                var totalConfirmed = caseEntity.Payments
                    .Where(p => p.Status == PaymentStatus.Completed)
                    .Sum(p => p.Amount);
                if (totalConfirmed < caseEntity.FinancialWorksheet.ApprovedAmount)
                {
                    errorMessage = ApiMessages.PaymentsIncomplete;
                    return false;
                }
                break;
        }

        errorMessage = string.Empty;
        return true;
    }

    public IEnumerable<WorkflowAction> GetAllowedActions(CaseStatus currentStatus, string userRole)
    {
        if (IsTerminalState(currentStatus))
            return [];

        if (string.Equals(userRole, SystemRoles.Admin, StringComparison.OrdinalIgnoreCase))
        {
            return Transitions
                .Where(x => x.Key.Current == currentStatus)
                .Select(x => x.Key.Action)
                .Append(WorkflowAction.Archive)
                .Distinct()
                .ToArray();
        }

        return Transitions
            .Where(x => x.Key.Current == currentStatus && string.Equals(x.Key.Role, userRole, StringComparison.OrdinalIgnoreCase))
            .Select(x => x.Key.Action)
            .Distinct()
            .ToArray();
    }
}