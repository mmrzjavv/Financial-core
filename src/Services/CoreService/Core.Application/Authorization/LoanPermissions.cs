namespace Core.Application.Authorization;

public static class LoanPermissions
{
    public const string Create = "loan_cases:create";
    public const string ReadOwn = "loan_cases:read_own";
    public const string ReadAll = "loan_cases:read_all";
    public const string ViewInternalComments = "loan_cases:view_internal_comments";
    public const string CreateInternalComment = "loan_cases:create_internal_comment";
    public const string ManageApprovalDetail = "loan_cases:manage_approval_detail";
    public const string ManageContracts = "loan_cases:manage_contracts";
    public const string ManageInstallments = "loan_cases:manage_installments";
    public const string ManagePayments = "loan_cases:manage_payments";
    public const string RepayInstallments = "loan_cases:repay_installments";
    public const string CeoApprove = "loan_cases:ceo_approve";
    public const string UploadDocuments = "loan_cases:upload_documents";
    public const string DownloadDocuments = "loan_cases:download_documents";
}
