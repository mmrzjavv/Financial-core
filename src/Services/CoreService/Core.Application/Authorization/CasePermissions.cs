namespace Core.Application.Authorization;

public static class CasePermissions
{
    public const string Create = "cases:create";
    public const string ReadOwn = "cases:read_own";
    public const string ReadAll = "cases:read_all";

    public const string ViewInternalComments = "cases:view_internal_comments";
    public const string CreateInternalComment = "cases:create_internal_comment";

    public const string ViewEvaluations = "cases:view_evaluations";
    public const string UpsertEvaluations = "cases:upsert_evaluations";

    public const string ManageValuations = "cases:manage_valuations";
    public const string ManageContracts = "cases:manage_contracts";
    public const string ManageFinancialWorksheet = "cases:manage_financial_worksheet";
    public const string ManagePayments = "cases:manage_payments";

    public const string UploadDocuments = "cases:upload_documents";
    public const string DownloadDocuments = "cases:download_documents";
    public const string UploadCommentAttachments = "cases:upload_comment_attachments";
}
