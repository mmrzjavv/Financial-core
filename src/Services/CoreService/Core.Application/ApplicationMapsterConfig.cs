using Core.Application.DTOs;
using Core.Application.Identity.DTOs.User;
using Core.Application.Requests;
using Mapster;
using Services.CoreService.Core.Domain.Entities;
using Services.CoreService.Core.Domain.Identity.Entities;

namespace Core.Application;

public sealed class ApplicationMapsterConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<Company, CompanyDto>();

        config.NewConfig<CaseDocument, CaseDocumentApplicantDto>();
        config.NewConfig<CaseDocument, CaseDocumentInternalDto>();

        config.NewConfig<InvestmentCase, InvestmentCaseApplicantDto>()
            .Map(dest => dest.Company, src => src.ApplicantCompany);
        config.NewConfig<InvestmentCase, InvestmentCaseInternalDto>()
            .Map(dest => dest.Company, src => src.ApplicantCompany);

        config.NewConfig<CaseCommentAttachment, CaseCommentAttachmentApplicantDto>();
        config.NewConfig<CaseCommentAttachment, CaseCommentAttachmentInternalDto>();

        config.NewConfig<CaseComment, CaseCommentApplicantDto>()
            .Map(dest => dest.Attachments, src => src.Attachments);
        config.NewConfig<CaseComment, CaseCommentInternalDto>()
            .Map(dest => dest.Attachments, src => src.Attachments);

        config.NewConfig<CaseWorkflowHistory, CaseWorkflowHistoryApplicantDto>();
        config.NewConfig<CaseWorkflowHistory, CaseWorkflowHistoryInternalDto>();

        config.NewConfig<CaseEvaluationItem, CaseEvaluationItemDto>();
        config.NewConfig<CaseEvaluation, CaseEvaluationDto>()
            .Map(dest => dest.Items, src => src.Items);

        config.NewConfig<User, UserDto>()
            .Map(dest => dest.RoleNumber, src => (int)src.Role)
            .Map(dest => dest.CreatedAt, src => src.CreatedAt);
    }
}
