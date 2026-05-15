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
        return applicant with { UpdatedAt = applicant.UpdatedAt ?? now, Company = company };
    }

    public CaseCommentDto MapComment(CaseComment comment)
        => authorizationService.IsInternalUser
            ? mapper.Map<CaseCommentInternalDto>(comment)
            : mapper.Map<CaseCommentApplicantDto>(comment);

    public CaseWorkflowHistoryDto MapHistory(CaseWorkflowHistory history)
        => authorizationService.IsInternalUser
            ? mapper.Map<CaseWorkflowHistoryInternalDto>(history)
            : mapper.Map<CaseWorkflowHistoryApplicantDto>(history);

    public CaseEvaluationDto MapEvaluation(CaseEvaluation evaluation)
        => mapper.Map<CaseEvaluationDto>(evaluation);
}
