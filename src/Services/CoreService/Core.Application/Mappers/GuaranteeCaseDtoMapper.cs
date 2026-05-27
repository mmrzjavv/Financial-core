using Core.Application.DTOs;
using Core.Application.Requests;
using Core.Domain.Entities;
using Core.Domain.Identity.Entities;

namespace Core.Application.Mappers;

public interface IGuaranteeCaseDtoMapper
{
    GuaranteeCaseDto MapCase(
        GuaranteeCase entity,
        bool isInternalView,
        Company? company = null,
        GuaranteeApplicantCreditSnapshotDto? applicantCreditSnapshot = null);
    GuaranteeApplicationDto? MapApplication(GuaranteeCaseApplication? application);
    GuaranteeApprovalFormDto? MapApprovalForm(GuaranteeApprovalForm? form);
    GuaranteeCaseDocumentDto MapDocument(GuaranteeCaseDocument document);
    GuaranteeCaseCommentDto MapComment(GuaranteeCaseComment comment);
    GuaranteeWorkflowHistoryDto MapHistory(GuaranteeCaseWorkflowHistory history);
    GuaranteeRenewalDto MapRenewal(GuaranteeRenewalCase renewal);
}

public sealed class GuaranteeCaseDtoMapper : IGuaranteeCaseDtoMapper
{
    public GuaranteeCaseDto MapCase(
        GuaranteeCase entity,
        bool isInternalView,
        Company? company = null,
        GuaranteeApplicantCreditSnapshotDto? applicantCreditSnapshot = null)
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

        if (isInternalView)
        {
            return new GuaranteeCaseInternalDto(
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
                MapApplication(entity.Application),
                MapApprovalForm(entity.ApprovalForm),
                applicantCreditSnapshot);
        }

        return new GuaranteeCaseApplicantDto(
            entity.Id,
            entity.CaseNumber,
            entity.ApplicantType,
            entity.CurrentPhase,
            entity.CurrentStatus,
            entity.CreatedAt,
            entity.UpdatedAt,
            entity.CompletedAt,
            companyDto,
            MapApplication(entity.Application),
            MapApprovalForm(entity.ApprovalForm),
            applicantCreditSnapshot);
    }

    public GuaranteeApplicationDto? MapApplication(GuaranteeCaseApplication? application)
    {
        if (application is null) return null;

        return new GuaranteeApplicationDto(
            application.GuaranteeType,
            application.ContractSubject,
            application.IsKnowledgeBasedProduct,
            application.BeneficiaryName,
            application.BeneficiaryNationalId,
            application.BeneficiaryCompanyType,
            application.ApplicantCategory,
            application.ApplicantCategoryOther,
            application.ApplicantLegalForm,
            application.BaseContractNumber,
            application.BaseContractAmount,
            application.BaseContractAmountInWords,
            application.PriceAdjustmentRatePercent,
            application.ExecutionProvince,
            application.RequestedGuaranteeAmount,
            application.InitialValidityDays,
            application.ValidityFrom,
            application.ValidityTo,
            application.CollateralDescription,
            application.FacilitySubject);
    }

    public GuaranteeApprovalFormDto? MapApprovalForm(GuaranteeApprovalForm? form)
    {
        if (form is null) return null;

        return new GuaranteeApprovalFormDto(
            form.CreditLimitWithCheck,
            form.FundIssuedGuaranteesTotal,
            form.ActiveCommitments,
            form.RemainingCredit,
            form.GuaranteeType,
            form.GuaranteeAmount,
            form.GuaranteeAmountInWords,
            form.ContractSubject,
            form.Beneficiary,
            form.IssuanceDate,
            form.ExpiryDate,
            form.ActiveDurationDays,
            form.DepositRatePercent,
            form.DepositAmount,
            form.AnnualCommissionRatePercent,
            form.CommissionAmount,
            form.CollateralDescription,
            form.GuarantorsDescription,
            form.OtherNotes);
    }

    public GuaranteeCaseDocumentDto MapDocument(GuaranteeCaseDocument document)
        => new(
            document.Id,
            document.DocumentType,
            document.FileName,
            document.MimeType,
            document.FileSize,
            document.Version,
            document.UploadedAt);

    public GuaranteeCaseCommentDto MapComment(GuaranteeCaseComment comment)
        => new(
            comment.Id,
            comment.Phase,
            comment.SenderUserId,
            comment.SenderRole,
            comment.Message,
            comment.IsRevisionRequest,
            comment.IsInternal,
            comment.CreatedAt);

    public GuaranteeWorkflowHistoryDto MapHistory(GuaranteeCaseWorkflowHistory history)
        => new(
            history.Id,
            history.FromPhase,
            history.ToPhase,
            history.FromStatus,
            history.ToStatus,
            history.ChangedByUserId,
            history.Action,
            history.ActorRole,
            history.Comment,
            history.CreatedAt);

    public GuaranteeRenewalDto MapRenewal(GuaranteeRenewalCase renewal)
        => new(
            renewal.Id,
            renewal.CaseNumber,
            renewal.ParentGuaranteeCaseId,
            renewal.ParentGuaranteeCase.CaseNumber,
            renewal.RenewalKind,
            renewal.CurrentStatus,
            renewal.RequestedExpiryDate,
            renewal.RequestedAmount,
            renewal.ApprovedExpiryDate,
            renewal.CreatedAt,
            renewal.UpdatedAt,
            renewal.CompletedAt);
}
