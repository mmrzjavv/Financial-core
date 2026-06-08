using BuildingBlocks.Domain.Abstractions;
using BuildingBlocks.Domain.Entities;
using Core.Domain.Enums;

namespace Core.Domain.Entities.Loan;

public sealed class LoanApprovalDetail : Entity<Guid>, IAuditableEntity
{
    private LoanApprovalDetail()
    {
    }

    public LoanApprovalDetail(Guid caseId)
    {
        Id = Guid.NewGuid();
        CaseId = caseId;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public Guid CaseId { get; private set; }
    public LoanCase Case { get; private set; } = default!;

    public decimal? DebtToAssetRatio { get; private set; }
    public decimal? CurrentRatio { get; private set; }
    public decimal? ProfitabilityRatioPercent { get; private set; }
    public decimal? CreditLimitWithCheck { get; private set; }
    public bool? IsCreditLineActive { get; private set; }
    public decimal? RemainingCreditAfterGrant { get; private set; }

    public LoanFacilityType? FacilityType { get; private set; }
    public string? ContractSubject { get; private set; }
    public string? BrokerageAndRelatedContract { get; private set; }
    public decimal? ApprovedAmount { get; private set; }
    public string? ApprovedAmountInWords { get; private set; }
    public int? RepaymentMonths { get; private set; }
    public int? GracePeriodMonths { get; private set; }
    public decimal? AnnualProfitRatePercent { get; private set; }
    public decimal? DailyPenaltyRatePercent { get; private set; }
    public string? CollateralDescription { get; private set; }
    public string? GuarantorsDescription { get; private set; }
    public string? OtherNotes { get; private set; }
    public decimal? ExpectedTotalProfit { get; private set; }
    public decimal? RepaymentCheckAmount { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? UpdatedAt { get; private set; }

    public void Update(
        decimal? debtToAssetRatio,
        decimal? currentRatio,
        decimal? profitabilityRatioPercent,
        decimal? creditLimitWithCheck,
        bool? isCreditLineActive,
        decimal? remainingCreditAfterGrant,
        LoanFacilityType? facilityType,
        string? contractSubject,
        string? brokerageAndRelatedContract,
        decimal? approvedAmount,
        string? approvedAmountInWords,
        int? repaymentMonths,
        int? gracePeriodMonths,
        decimal? annualProfitRatePercent,
        decimal? dailyPenaltyRatePercent,
        string? collateralDescription,
        string? guarantorsDescription,
        string? otherNotes,
        decimal? expectedTotalProfit,
        decimal? repaymentCheckAmount)
    {
        DebtToAssetRatio = debtToAssetRatio;
        CurrentRatio = currentRatio;
        ProfitabilityRatioPercent = profitabilityRatioPercent;
        CreditLimitWithCheck = creditLimitWithCheck;
        IsCreditLineActive = isCreditLineActive;
        RemainingCreditAfterGrant = remainingCreditAfterGrant;
        FacilityType = facilityType;
        ContractSubject = contractSubject;
        BrokerageAndRelatedContract = brokerageAndRelatedContract;
        ApprovedAmount = approvedAmount;
        ApprovedAmountInWords = approvedAmountInWords;
        RepaymentMonths = repaymentMonths;
        GracePeriodMonths = gracePeriodMonths;
        AnnualProfitRatePercent = annualProfitRatePercent;
        DailyPenaltyRatePercent = dailyPenaltyRatePercent;
        CollateralDescription = collateralDescription;
        GuarantorsDescription = guarantorsDescription;
        OtherNotes = otherNotes;
        ExpectedTotalProfit = expectedTotalProfit;
        RepaymentCheckAmount = repaymentCheckAmount;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
