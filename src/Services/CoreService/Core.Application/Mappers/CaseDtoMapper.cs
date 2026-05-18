using Core.Application.Authorization;
using Core.Application.DTOs;
using Core.Application.Requests;
using MapsterMapper;
using Services.CoreService.Core.Domain.Entities;
using Services.CoreService.Core.Domain.Identity.Entities;

namespace Core.Application.Mappers;

public interface ICaseDtoMapper
{
    CompanyDto? MapCompany(Company? company);
    CaseDocumentDto MapDocument(CaseDocument document);
    InvestmentCaseDto MapCase(InvestmentCase entity, DateTimeOffset now, bool isInternalView, Company? companyOverride = null);
    CaseCommentDto MapComment(CaseComment comment);
    CaseWorkflowHistoryDto MapHistory(CaseWorkflowHistory history);
    CaseEvaluationDto MapEvaluation(CaseEvaluation evaluation);
}

public sealed class CaseDtoMapper(IMapper mapper, ICaseAuthorizationService authorizationService) : ICaseDtoMapper
{
    public CompanyDto? MapCompany(Company? company)
        => company is null ? null : mapper.Map<CompanyDto>(company);

    public CaseDocumentDto MapDocument(CaseDocument document)
        => authorizationService.IsInternalUser
            ? mapper.Map<CaseDocumentInternalDto>(document)
            : mapper.Map<CaseDocumentApplicantDto>(document);

    public InvestmentCaseDto MapCase(InvestmentCase entity, DateTimeOffset now, bool isInternalView, Company? companyOverride = null)
    {
        var company = MapCompany(companyOverride ?? entity.ApplicantCompany);

        if (isInternalView)
        {
            var dto = mapper.Map<InvestmentCaseInternalDto>(entity);
            return dto with { UpdatedAt = dto.UpdatedAt ?? now, Company = company };
        }

        var applicant = mapper.Map<InvestmentCaseApplicantDto>(entity);
        return applicant with
        {
            UpdatedAt = applicant.UpdatedAt ?? now,
            Company = company,
            DataEntry1 = MapDataEntry1(entity.DataEntry1),
            DataEntry2 = MapDataEntry2(entity.DataEntry2)
        };
    }

    private static DataEntry1Dto? MapDataEntry1(InvestmentCaseDataEntry1? dataEntry)
        => dataEntry is null
            ? null
            : new DataEntry1Dto(
                dataEntry.StartupTitle,
                dataEntry.BusinessDescription,
                dataEntry.RequestedAmount,
                dataEntry.TeamSize,
                dataEntry.Website,
                dataEntry.Country,
                dataEntry.City);

    private static DataEntry2Dto? MapDataEntry2(InvestmentCaseDataEntry2? dataEntry)
        => dataEntry is null
            ? null
            : new DataEntry2Dto(
                dataEntry.MarketAnalysis,
                dataEntry.RevenueModel,
                dataEntry.CompetitiveAdvantage,
                dataEntry.FinancialProjection);

    public CaseCommentDto MapComment(CaseComment comment)
    {
        var attachments = comment.Attachments.Select(MapCommentAttachment).ToArray();

        if (authorizationService.IsInternalUser)
        {
            return new CaseCommentInternalDto(
                comment.Id,
                comment.CaseId,
                comment.Phase,
                comment.SenderUserId,
                comment.SenderRole,
                comment.Message,
                comment.IsRevisionRequest,
                comment.IsInternal,
                comment.ParentId,
                attachments,
                comment.CreatedAt);
        }

        return new CaseCommentApplicantDto(
            comment.Id,
            comment.CaseId,
            comment.Phase,
            comment.Message,
            comment.IsRevisionRequest,
            comment.ParentId,
            attachments,
            comment.CreatedAt);
    }

    private CaseCommentAttachmentDto MapCommentAttachment(CaseCommentAttachment attachment)
        => authorizationService.IsInternalUser
            ? new CaseCommentAttachmentInternalDto(attachment.Id, attachment.S3Key, attachment.FileName)
            : new CaseCommentAttachmentApplicantDto(attachment.Id, attachment.FileName);

    public CaseWorkflowHistoryDto MapHistory(CaseWorkflowHistory history)
        => authorizationService.IsInternalUser
            ? mapper.Map<CaseWorkflowHistoryInternalDto>(history)
            : mapper.Map<CaseWorkflowHistoryApplicantDto>(history);

    public CaseEvaluationDto MapEvaluation(CaseEvaluation evaluation)
        => mapper.Map<CaseEvaluationDto>(evaluation);
}
