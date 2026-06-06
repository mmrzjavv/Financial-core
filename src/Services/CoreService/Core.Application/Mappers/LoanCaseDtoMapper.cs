using Core.Application.DTOs;
using Core.Application.Requests;
using Core.Domain.Entities;
using Core.Domain.Identity.Entities;

namespace Core.Application.Mappers;

public sealed class LoanCaseDtoMapper : ILoanCaseDtoMapper
{
    public LoanCaseDto MapCase(
        LoanCase entity,
        bool isInternalUser,
        Company? company,
        bool includeInstallments = false,
        bool includePayments = false)
    {
        var companyDto = company is null ? null : new CompanyDto(
            company.Id,
            company.Name,
            company.EconomicCode,
            company.RegistrationNumber,
            company.NationalId,
            company.PhoneNumber,
            company.Address,
            company.City,
            company.Province,
            company.PostalCode);

        IReadOnlyList<LoanInstallmentDto>? installments = includeInstallments
            ? entity.Installments.OrderBy(x => x.RowNumber).Select(MapInstallment).ToList()
            : null;

        IReadOnlyList<LoanPaymentDto>? payments = includePayments
            ? entity.Payments.OrderBy(x => x.StageNumber).Select(MapPayment).ToList()
            : null;

        if (isInternalUser)
        {
            return new LoanCaseInternalDto(
                entity.Id,
                entity.CaseNumber,
                entity.ApplicantUserId,
                entity.ApplicantType,
                entity.CurrentPhase,
                entity.CurrentStatus,
                entity.WorkflowInstanceId,
                entity.CreatedAt,
                entity.UpdatedAt,
                entity.CompletedAt,
                companyDto,
                entity.Application is null ? null : MapApplication(entity.Application),
                entity.ApprovalDetail is null ? null : MapApprovalDetail(entity.ApprovalDetail),
                installments,
                payments);
        }

        return new LoanCaseApplicantDto(
            entity.Id,
            entity.CaseNumber,
            entity.ApplicantType,
            entity.CurrentPhase,
            entity.CurrentStatus,
            entity.CreatedAt,
            entity.UpdatedAt,
            entity.CompletedAt,
            companyDto,
            entity.Application is null ? null : MapApplication(entity.Application),
            entity.ApprovalDetail is null ? null : MapApprovalDetail(entity.ApprovalDetail),
            installments,
            payments);
    }

    public LoanApplicationDto MapApplication(LoanCaseApplication application) =>
        new(
            application.RequestedAmount,
            application.RequestedAmountInWords,
            application.FacilitySubject,
            application.OfferedGuarantees,
            application.ApplicantCategory,
            application.ApplicantCategoryOther,
            application.RepresentativePosition);

    public LoanApprovalDetailDto MapApprovalDetail(LoanApprovalDetail detail) =>
        new(
            detail.DebtToAssetRatio,
            detail.CurrentRatio,
            detail.ProfitabilityRatioPercent,
            detail.CreditLimitWithCheck,
            detail.IsCreditLineActive,
            detail.RemainingCreditAfterGrant,
            detail.FacilityType,
            detail.ContractSubject,
            detail.BrokerageAndRelatedContract,
            detail.ApprovedAmount,
            detail.ApprovedAmountInWords,
            detail.RepaymentMonths,
            detail.GracePeriodMonths,
            detail.AnnualProfitRatePercent,
            detail.DailyPenaltyRatePercent,
            detail.CollateralDescription,
            detail.GuarantorsDescription,
            detail.OtherNotes,
            detail.ExpectedTotalProfit,
            detail.RepaymentCheckAmount);

    public LoanCaseDocumentDto MapDocument(LoanCaseDocument document) =>
        new(
            document.Id,
            document.DocumentType,
            document.FileName,
            document.MimeType,
            document.FileSize,
            document.Version,
            document.UploadedByUserId,
            document.UploadedAt);

    public LoanInstallmentDto MapInstallment(LoanInstallment installment) =>
        new(
            installment.Id,
            installment.RowNumber,
            installment.InstallmentDate,
            installment.PrincipalAmount,
            installment.ProfitAmount,
            installment.TotalAmount,
            installment.FundShareOfPrincipal,
            installment.FundShareOfProfit,
            installment.FundShareOfTotal,
            installment.IsGracePeriod,
            installment.IsPaid,
            installment.PaidAt);

    public LoanPaymentDto MapPayment(LoanPayment payment) =>
        new(
            payment.Id,
            payment.Amount,
            payment.PaymentDate,
            payment.TransactionNumber,
            payment.ReceiptS3Key,
            payment.Notes,
            payment.StageNumber,
            payment.CreatedByUserId,
            payment.CreatedAt);

    public LoanWorkflowHistoryDto MapHistory(LoanCaseWorkflowHistory history) =>
        new(
            history.Id,
            history.FromPhase,
            history.ToPhase,
            history.FromStatus,
            history.ToStatus,
            history.ChangedByUserId,
            history.Action,
            history.ActorRole,
            history.CorrelationId,
            history.Comment,
            history.CreatedAt);

    public LoanCaseCommentDto MapComment(LoanCaseComment comment) =>
        new(
            comment.Id,
            comment.Phase,
            comment.SenderUserId,
            comment.SenderRole,
            comment.Message,
            comment.IsRevisionRequest,
            comment.IsInternal,
            comment.ParentId,
            comment.CreatedAt);
}
