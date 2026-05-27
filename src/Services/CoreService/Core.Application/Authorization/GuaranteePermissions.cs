namespace Core.Application.Authorization;

public static class GuaranteePermissions
{
    public const string Create = "guarantee_cases:create";
    public const string ReadOwn = "guarantee_cases:read_own";
    public const string ReadAll = "guarantee_cases:read_all";
    public const string ViewInternalComments = "guarantee_cases:view_internal_comments";
    public const string CreateInternalComment = "guarantee_cases:create_internal_comment";
    public const string ManageApprovalForm = "guarantee_cases:manage_approval_form";
    public const string ManageContracts = "guarantee_cases:manage_contracts";
    public const string ManageAttachments = "guarantee_cases:manage_attachments";
    public const string ManageIssuance = "guarantee_cases:manage_issuance";
    public const string CeoApprove = "guarantee_cases:ceo_approve";
    public const string SetApplicantCreditLimit = "guarantee_cases:set_applicant_credit_limit";
    public const string UploadDocuments = "guarantee_cases:upload_documents";
    public const string DownloadDocuments = "guarantee_cases:download_documents";
}
