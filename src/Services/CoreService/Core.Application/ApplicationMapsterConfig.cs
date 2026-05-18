using Core.Application.DTOs;
using Core.Application.Identity.DTOs.User;
using Core.Application.Requests;
using Core.Domain.Entities;
using Core.Domain.Identity.Entities;
using Mapster;

namespace Core.Application;

public sealed class ApplicationMapsterConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<Company, CompanyDto>();

        config.NewConfig<CaseDocument, CaseDocumentApplicantDto>();
        config.NewConfig<CaseDocument, CaseDocumentInternalDto>();

        config.NewConfig<InvestmentCaseDataEntry1, DataEntry1Dto>();
        config.NewConfig<InvestmentCaseDataEntry2, DataEntry2Dto>();

        config.NewConfig<InvestmentCase, InvestmentCaseApplicantDto>()
            .Map(dest => dest.Company, src => src.ApplicantCompany)
            .Map(dest => dest.DataEntry1, src => src.DataEntry1 != null ? src.DataEntry1.Adapt<DataEntry1Dto>() : null)
            .Map(dest => dest.DataEntry2, src => src.DataEntry2 != null ? src.DataEntry2.Adapt<DataEntry2Dto>() : null);
        config.NewConfig<InvestmentCase, InvestmentCaseInternalDto>()
            .Map(dest => dest.Company, src => src.ApplicantCompany);

        config.NewConfig<CaseCommentAttachment, CaseCommentAttachmentApplicantDto>();
        config.NewConfig<CaseCommentAttachment, CaseCommentAttachmentInternalDto>();

        config.NewConfig<CaseComment, CaseCommentApplicantDto>()
            .Ignore(dest => dest.Attachments);
        config.NewConfig<CaseComment, CaseCommentInternalDto>()
            .Ignore(dest => dest.Attachments);

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
