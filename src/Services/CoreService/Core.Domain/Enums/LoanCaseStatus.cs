namespace Core.Domain.Enums;

public enum LoanCaseStatus
{
    Draft = 1,
    DataEntry = 2,
    PendingCreditReview = 3,
    RevisionRequestedByCredit = 4,
    PendingCeoInitialApproval = 5,
    CanceledByCeo = 6,
    PendingLegalRawContract = 7,
    PendingApplicantSignature = 8,
    PendingLegalFinalReview = 9,
    RevisionRequestedByLegal = 10,
    PendingFinancialReview = 11,
    RevisionRequestedByFinancial = 12,
    PendingLegalFinalContract = 13,
    PendingCeoFinalApproval = 14,
    ReadyForPayment = 15,
    RepaymentPhase = 16,
    Completed = 17,
    Archived = 18
}
