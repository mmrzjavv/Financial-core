namespace Services.CoreService.Core.Domain.Enums;

public enum CaseStatus
{
    Draft = 1,
    DataEntry1 = 2,
    ReviewDataEntry1 = 3,
    DataEntry2 = 4,
    ReviewDataEntry2 = 5,
    InitialValuation = 6,
    SecondaryValuation = 7,
    WaitingPreliminaryContract = 8,
    WaitingUserReviewPreliminaryContract = 9,
    ContractDrafting = 10,
    WaitingContractSignature = 11,
    WaitingSignedContractUpload = 12,
    WaitingFinancialWorksheet = 13,
    FinancialWorksheetReview = 14,
    WaitingPayment = 15,
    Completed = 16,
    Rejected = 17,
    Cancelled = 18,
    Archived = 19
}