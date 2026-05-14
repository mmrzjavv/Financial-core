using BuildingBlocks.Application.Errors;
using BuildingBlocks.Application.Results;
using Services.CoreService.Core.Application.Abstractions;
using Services.CoreService.Core.Domain.Enums;

namespace Services.CoreService.Core.Application.State;

public sealed class CaseStateManager : ICaseStateManager
{
    private static readonly HashSet<(CasePhase FromPhase, CaseStatus FromStatus, CasePhase ToPhase, CaseStatus ToStatus)> Allowed =
    [
        (CasePhase.DataEntry1, CaseStatus.Draft, CasePhase.DataEntry1, CaseStatus.Submitted),
        (CasePhase.DataEntry1, CaseStatus.RevisionRequested, CasePhase.DataEntry1, CaseStatus.Submitted),

        (CasePhase.DataEntry1, CaseStatus.Submitted, CasePhase.ExpertReview, CaseStatus.WaitingForReview),
        (CasePhase.ExpertReview, CaseStatus.WaitingForReview, CasePhase.InvestmentManagerReview, CaseStatus.WaitingForReview),
        (CasePhase.InvestmentManagerReview, CaseStatus.WaitingForReview, CasePhase.DataEntry2, CaseStatus.InProgress),

        (CasePhase.DataEntry2, CaseStatus.InProgress, CasePhase.DataEntry2, CaseStatus.Submitted),
        (CasePhase.DataEntry2, CaseStatus.RevisionRequested, CasePhase.DataEntry2, CaseStatus.Submitted),

        (CasePhase.DataEntry2, CaseStatus.Submitted, CasePhase.ManagerReview, CaseStatus.WaitingForReview),
        (CasePhase.ManagerReview, CaseStatus.WaitingForReview, CasePhase.PrimaryValuation, CaseStatus.InProgress),

        (CasePhase.PrimaryValuation, CaseStatus.InProgress, CasePhase.SecondaryValuation, CaseStatus.InProgress),
        (CasePhase.SecondaryValuation, CaseStatus.InProgress, CasePhase.FinancialWorksheet, CaseStatus.InProgress),

        (CasePhase.FinancialWorksheet, CaseStatus.InProgress, CasePhase.FinancialWorksheet, CaseStatus.Submitted),
        (CasePhase.FinancialWorksheet, CaseStatus.RevisionRequested, CasePhase.FinancialWorksheet, CaseStatus.Submitted),

        (CasePhase.FinancialWorksheet, CaseStatus.Submitted, CasePhase.FinanceReview, CaseStatus.WaitingForReview),
        (CasePhase.FinanceReview, CaseStatus.WaitingForReview, CasePhase.CEOApproval, CaseStatus.WaitingForReview),

        (CasePhase.CEOApproval, CaseStatus.WaitingForReview, CasePhase.LegalPreContract, CaseStatus.InProgress),

        (CasePhase.LegalPreContract, CaseStatus.InProgress, CasePhase.LegalPreContract, CaseStatus.Submitted),
        (CasePhase.LegalPreContract, CaseStatus.RevisionRequested, CasePhase.LegalPreContract, CaseStatus.Submitted),

        (CasePhase.LegalPreContract, CaseStatus.Submitted, CasePhase.UserReviewLoop, CaseStatus.WaitingForReview),
        (CasePhase.UserReviewLoop, CaseStatus.WaitingForReview, CasePhase.FinalContractUpload, CaseStatus.InProgress),
        (CasePhase.FinalContractUpload, CaseStatus.InProgress, CasePhase.SignatureScheduling, CaseStatus.InProgress),
        (CasePhase.SignatureScheduling, CaseStatus.InProgress, CasePhase.SignedContractUpload, CaseStatus.InProgress),
        (CasePhase.SignedContractUpload, CaseStatus.InProgress, CasePhase.PaymentProcessing, CaseStatus.InProgress),
        (CasePhase.PaymentProcessing, CaseStatus.InProgress, CasePhase.Completion, CaseStatus.Completed)
    ];

    public Result ValidateTransition(CasePhase fromPhase, CaseStatus fromStatus, CasePhase toPhase, CaseStatus toStatus)
    {
        if (Allowed.Contains((fromPhase, fromStatus, toPhase, toStatus)))
            return Result.Ok();

        return Result.Fail(Error.Conflict($"Invalid transition: {fromPhase}/{fromStatus} -> {toPhase}/{toStatus}."));
    }
}

