using BuildingBlocks.Application.Errors;
using BuildingBlocks.Application.Results;
using Core.Application.Abstractions;
using Core.Application.Common;
using Core.Domain.Constants;
using Core.Domain.Entities;
using Core.Domain.Enums;
using Core.Domain.Identity;

namespace Core.Application.Services;

public sealed class GuaranteeCaseStateManager : IGuaranteeCaseStateManager
{
    private static readonly HashSet<GuaranteeCaseStatus> TerminalStates =
    [
        GuaranteeCaseStatus.Rejected,
        GuaranteeCaseStatus.Cancelled,
        GuaranteeCaseStatus.Archived,
        GuaranteeCaseStatus.Completed
    ];

    private static readonly Dictionary<(GuaranteeCaseStatus Current, GuaranteeWorkflowAction Action, string Role), GuaranteeCaseStatus> Transitions = new()
    {
        { (GuaranteeCaseStatus.Draft, GuaranteeWorkflowAction.Submit, UserRoleClaims.Applicant), GuaranteeCaseStatus.DataEntry },

        { (GuaranteeCaseStatus.DataEntry, GuaranteeWorkflowAction.Submit, UserRoleClaims.Applicant), GuaranteeCaseStatus.CreditReview },
        { (GuaranteeCaseStatus.DataEntry, GuaranteeWorkflowAction.Cancel, UserRoleClaims.Applicant), GuaranteeCaseStatus.Cancelled },

        { (GuaranteeCaseStatus.CreditReview, GuaranteeWorkflowAction.Approve, UserRoleClaims.CreditExpert), GuaranteeCaseStatus.ApprovalFormEntry },
        { (GuaranteeCaseStatus.CreditReview, GuaranteeWorkflowAction.RequestRevision, UserRoleClaims.CreditExpert), GuaranteeCaseStatus.DataEntry },
        { (GuaranteeCaseStatus.CreditReview, GuaranteeWorkflowAction.Reject, UserRoleClaims.CreditExpert), GuaranteeCaseStatus.Rejected },

        { (GuaranteeCaseStatus.ApprovalFormEntry, GuaranteeWorkflowAction.Submit, UserRoleClaims.CreditExpert), GuaranteeCaseStatus.CeoApprovalInitial },
        { (GuaranteeCaseStatus.ApprovalFormEntry, GuaranteeWorkflowAction.Reject, UserRoleClaims.CreditExpert), GuaranteeCaseStatus.Rejected },

        { (GuaranteeCaseStatus.CeoApprovalInitial, GuaranteeWorkflowAction.Approve, UserRoleClaims.Ceo), GuaranteeCaseStatus.WaitingDraftContract },
        { (GuaranteeCaseStatus.CeoApprovalInitial, GuaranteeWorkflowAction.Reject, UserRoleClaims.Ceo), GuaranteeCaseStatus.Rejected },
        { (GuaranteeCaseStatus.CeoApprovalInitial, GuaranteeWorkflowAction.Cancel, UserRoleClaims.Ceo), GuaranteeCaseStatus.Cancelled },

        { (GuaranteeCaseStatus.WaitingDraftContract, GuaranteeWorkflowAction.UploadDraftContract, UserRoleClaims.LegalExpert), GuaranteeCaseStatus.WaitingSignedContractAndAttachments },
        { (GuaranteeCaseStatus.WaitingDraftContract, GuaranteeWorkflowAction.Reject, UserRoleClaims.LegalExpert), GuaranteeCaseStatus.Rejected },

        { (GuaranteeCaseStatus.WaitingSignedContractAndAttachments, GuaranteeWorkflowAction.SubmitSignedPackage, UserRoleClaims.Applicant), GuaranteeCaseStatus.FinancialAttachmentReview },
        { (GuaranteeCaseStatus.WaitingSignedContractAndAttachments, GuaranteeWorkflowAction.Cancel, UserRoleClaims.Applicant), GuaranteeCaseStatus.Cancelled },

        { (GuaranteeCaseStatus.FinancialAttachmentReview, GuaranteeWorkflowAction.ApproveAttachments, UserRoleClaims.FinancialExpert), GuaranteeCaseStatus.WaitingFinalContract },
        { (GuaranteeCaseStatus.FinancialAttachmentReview, GuaranteeWorkflowAction.RequestRevision, UserRoleClaims.FinancialExpert), GuaranteeCaseStatus.WaitingSignedContractAndAttachments },
        { (GuaranteeCaseStatus.FinancialAttachmentReview, GuaranteeWorkflowAction.Reject, UserRoleClaims.FinancialExpert), GuaranteeCaseStatus.Rejected },

        { (GuaranteeCaseStatus.WaitingFinalContract, GuaranteeWorkflowAction.UploadFinalContract, UserRoleClaims.LegalExpert), GuaranteeCaseStatus.CeoApprovalFinal },
        { (GuaranteeCaseStatus.WaitingFinalContract, GuaranteeWorkflowAction.Reject, UserRoleClaims.LegalExpert), GuaranteeCaseStatus.Rejected },

        { (GuaranteeCaseStatus.CeoApprovalFinal, GuaranteeWorkflowAction.Approve, UserRoleClaims.Ceo), GuaranteeCaseStatus.WaitingIssuanceDocuments },
        { (GuaranteeCaseStatus.CeoApprovalFinal, GuaranteeWorkflowAction.Reject, UserRoleClaims.Ceo), GuaranteeCaseStatus.Rejected },
        { (GuaranteeCaseStatus.CeoApprovalFinal, GuaranteeWorkflowAction.Cancel, UserRoleClaims.Ceo), GuaranteeCaseStatus.Cancelled },

        { (GuaranteeCaseStatus.WaitingIssuanceDocuments, GuaranteeWorkflowAction.UploadIssuanceDocuments, UserRoleClaims.FinancialExpert), GuaranteeCaseStatus.Completed },
        { (GuaranteeCaseStatus.WaitingIssuanceDocuments, GuaranteeWorkflowAction.Reject, UserRoleClaims.FinancialExpert), GuaranteeCaseStatus.Rejected }
    };

    static GuaranteeCaseStateManager()
    {
        WorkflowRoleExpander.MirrorUnitManager(Transitions, UserRoleClaims.CreditExpert, UserRoleClaims.CreditManager);
        WorkflowRoleExpander.MirrorUnitManager(Transitions, UserRoleClaims.LegalExpert, UserRoleClaims.LegalManager);
        WorkflowRoleExpander.MirrorUnitManager(Transitions, UserRoleClaims.FinancialExpert, UserRoleClaims.FinancialManager);
    }

    public bool IsTerminalState(GuaranteeCaseStatus status) => TerminalStates.Contains(status);

    public bool CanTransition(
        GuaranteeCaseStatus currentStatus,
        GuaranteeWorkflowAction action,
        string userRole,
        out GuaranteeCaseStatus nextStatus,
        out string errorMessage)
    {
        nextStatus = currentStatus;

        if (IsTerminalState(currentStatus))
        {
            errorMessage = ApiMessages.CannotTransitionFromTerminalState;
            return false;
        }

        if (action == GuaranteeWorkflowAction.Archive)
        {
            if (!string.Equals(userRole, UserRoleClaims.Admin, StringComparison.OrdinalIgnoreCase))
            {
                errorMessage = ApiMessages.OnlyAdminCanArchive;
                return false;
            }

            nextStatus = GuaranteeCaseStatus.Archived;
            errorMessage = string.Empty;
            return true;
        }

        if (Transitions.TryGetValue((currentStatus, action, userRole), out var directNext))
        {
            nextStatus = directNext;
            errorMessage = string.Empty;
            return true;
        }

        if (string.Equals(userRole, UserRoleClaims.Admin, StringComparison.OrdinalIgnoreCase))
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
        GuaranteeCase caseEntity,
        GuaranteeWorkflowAction action,
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

        if (action == GuaranteeWorkflowAction.Archive && caseEntity.CurrentStatus == GuaranteeCaseStatus.Archived)
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

        if (action == GuaranteeWorkflowAction.RequestRevision)
        {
            caseEntity.RequestRevision(nextStatus, actorId, actorRole, action, correlationId.Value, comment ?? string.Empty, isInternal: false);
            return Task.FromResult(Result.Ok());
        }

        caseEntity.TransitionTo(nextStatus, actorId, actorRole, action, correlationId.Value, comment);
        return Task.FromResult(Result.Ok());
    }

    private static bool ValidateBusinessRules(
        GuaranteeCase caseEntity,
        GuaranteeWorkflowAction action,
        GuaranteeCaseStatus nextStatus,
        out string errorMessage)
    {
        switch (action)
        {
            case GuaranteeWorkflowAction.Submit when caseEntity.CurrentStatus == GuaranteeCaseStatus.DataEntry:
                if (!GuaranteeApplicationCompleteness.IsComplete(caseEntity.Application))
                {
                    errorMessage = ApiMessages.GuaranteeApplicationIncomplete;
                    return false;
                }

                var application = caseEntity.Application!;
                var missingDocs = GuaranteeDocumentRequirements.GetMissingForDataEntrySubmit(
                    application.GuaranteeType,
                    caseEntity.Documents);
                if (missingDocs.Count > 0)
                {
                    errorMessage = GuaranteeDocumentRequirements.FormatDataEntryDocumentsIncompleteMessage(missingDocs);
                    return false;
                }
                break;

            case GuaranteeWorkflowAction.Submit when caseEntity.CurrentStatus == GuaranteeCaseStatus.ApprovalFormEntry:
                if (!GuaranteeApprovalFormCompleteness.IsComplete(caseEntity.ApprovalForm))
                {
                    errorMessage = ApiMessages.GuaranteeApprovalFormIncomplete;
                    return false;
                }
                break;

            case GuaranteeWorkflowAction.UploadDraftContract:
                if (!caseEntity.Documents.Any(x => !x.IsDeleted && x.DocumentType == GuaranteeDocumentType.DraftContract))
                {
                    errorMessage = ApiMessages.GuaranteeDraftContractMissing;
                    return false;
                }
                break;

            case GuaranteeWorkflowAction.SubmitSignedPackage:
                if (!caseEntity.Documents.Any(x => !x.IsDeleted && x.DocumentType == GuaranteeDocumentType.SignedContract))
                {
                    errorMessage = ApiMessages.GuaranteeSignedContractMissing;
                    return false;
                }
                break;

            case GuaranteeWorkflowAction.UploadFinalContract:
                if (!caseEntity.Documents.Any(x => !x.IsDeleted && x.DocumentType == GuaranteeDocumentType.FinalContract))
                {
                    errorMessage = ApiMessages.GuaranteeFinalContractMissing;
                    return false;
                }
                break;

            case GuaranteeWorkflowAction.UploadIssuanceDocuments:
                foreach (var required in GuaranteeDocumentRequirements.RequiredForIssuance)
                {
                    if (!caseEntity.Documents.Any(x => !x.IsDeleted && x.DocumentType == required))
                    {
                        errorMessage = ApiMessages.GuaranteeIssuanceDocumentsIncomplete;
                        return false;
                    }
                }
                break;
        }

        errorMessage = string.Empty;
        return true;
    }

    public IEnumerable<GuaranteeWorkflowAction> GetAllowedActions(GuaranteeCaseStatus currentStatus, string userRole)
    {
        if (IsTerminalState(currentStatus))
            return [];

        if (string.Equals(userRole, UserRoleClaims.Admin, StringComparison.OrdinalIgnoreCase))
        {
            return Transitions
                .Where(x => x.Key.Current == currentStatus)
                .Select(x => x.Key.Action)
                .Append(GuaranteeWorkflowAction.Archive)
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
