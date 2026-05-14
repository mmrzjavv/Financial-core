using System.Net;
using Asp.Versioning;
using BuildingBlocks.Application.Results;
using Core.API.Http;
using Core.Application.Abstractions;
using Core.Application.DTOs;
using Core.Application.Requests;
using Core.Application.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Core.API.Controllers;

[ApiController]
[ApiVersion(1.0)]
[Route("api/v{version:apiVersion}/cases")]
public sealed class InvestmentCasesController(IInvestmentCaseAppService service) : ControllerBase
{
    #region Core Management

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateInvestmentCaseRequest request, CancellationToken ct)
    {
        var result = await service.CreateAsync(request, ct);
        return ApiResponse.From(result, "Investment case created.", HttpStatusCode.Created);
    }

    [HttpGet("{id:guid}")]
    [Authorize]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await service.GetAsync(id, ct);
        return ApiResponse.From(result, "Investment case retrieved.");
    }

    [HttpGet]
    [Authorize]
    public async Task<IActionResult> Search([FromQuery] CaseSearchRequest request, CancellationToken ct)
    {
        var result = await service.SearchAsync(request, ct);
        return ApiResponse.From(result, "Investment cases retrieved.");
    }

    [HttpGet("{id:guid}/history")]
    [Authorize]
    public async Task<IActionResult> GetHistory(Guid id, CancellationToken ct)
    {
        var result = await service.GetHistoryAsync(id, ct);
        return ApiResponse.From(result, "Case history retrieved.");
    }

    #endregion

    #region Data Entry 1 & 2

    [HttpPut("{id:guid}/data-entry1")]
    [Authorize(Policy = "ApplicantOnly")]
    public async Task<IActionResult> UpdateDataEntry1(Guid id, [FromBody] UpdateDataEntry1Request request, CancellationToken ct)
    {
        var result = await service.UpdateDataEntry1Async(id, request, ct);
        return ApiResponse.From(result, "Data entry 1 updated.");
    }

    [HttpPost("{id:guid}/data-entry1/submit")]
    [Authorize(Policy = "ApplicantOnly")]
    public async Task<IActionResult> SubmitDataEntry1(Guid id, [FromBody] SemanticTransitionRequest request, CancellationToken ct)
    {
        var result = await service.SubmitDataEntry1Async(id, request.Comment, ct);
        return ApiResponse.From(result, "Data entry 1 submitted.", HttpStatusCode.Accepted);
    }

    [HttpPost("{id:guid}/data-entry1/approve")]
    [Authorize(Policy = "InternalOnly")]
    public async Task<IActionResult> ApproveDataEntry1(Guid id, [FromBody] SemanticTransitionRequest request, CancellationToken ct)
    {
        var result = await service.ApproveDataEntry1Async(id, request.Comment, ct);
        return ApiResponse.From(result, "Data entry 1 approved.", HttpStatusCode.Accepted);
    }

    [HttpPost("{id:guid}/data-entry1/revision-request")]
    [Authorize(Policy = "InternalOnly")]
    public async Task<IActionResult> RequestDataEntry1Revision(Guid id, [FromBody] SemanticRevisionRequest request, CancellationToken ct)
    {
        var result = await service.RequestDataEntry1RevisionAsync(id, request.Message, ct);
        return ApiResponse.From(result, "Data entry 1 revision requested.", HttpStatusCode.Accepted);
    }

    [HttpPut("{id:guid}/data-entry2")]
    [Authorize(Policy = "ApplicantOnly")]
    public async Task<IActionResult> UpdateDataEntry2(Guid id, [FromBody] UpdateDataEntry2Request request, CancellationToken ct)
    {
        var result = await service.UpdateDataEntry2Async(id, request, ct);
        return ApiResponse.From(result, "Data entry 2 updated.");
    }

    [HttpPost("{id:guid}/data-entry2/submit")]
    [Authorize(Policy = "ApplicantOnly")]
    public async Task<IActionResult> SubmitDataEntry2(Guid id, [FromBody] SemanticTransitionRequest request, CancellationToken ct)
    {
        var result = await service.SubmitDataEntry2Async(id, request.Comment, ct);
        return ApiResponse.From(result, "Data entry 2 submitted.", HttpStatusCode.Accepted);
    }

    [HttpPost("{id:guid}/data-entry2/approve")]
    [Authorize(Policy = "InternalOnly")]
    public async Task<IActionResult> ApproveDataEntry2(Guid id, [FromBody] SemanticTransitionRequest request, CancellationToken ct)
    {
        var result = await service.ApproveDataEntry2Async(id, request.Comment, ct);
        return ApiResponse.From(result, "Data entry 2 approved.", HttpStatusCode.Accepted);
    }

    [HttpPost("{id:guid}/data-entry2/revision-request")]
    [Authorize(Policy = "InternalOnly")]
    public async Task<IActionResult> RequestDataEntry2Revision(Guid id, [FromBody] SemanticRevisionRequest request, CancellationToken ct)
    {
        var result = await service.RequestDataEntry2RevisionAsync(id, request.Message, ct);
        return ApiResponse.From(result, "Data entry 2 revision requested.", HttpStatusCode.Accepted);
    }

    #endregion

    #region Valuations

    [HttpPost("{id:guid}/valuations")]
    [Authorize(Policy = "InternalOnly")]
    public async Task<IActionResult> RecordValuation(Guid id, [FromBody] RecordValuationRequest request, CancellationToken ct)
    {
        var result = await service.RecordValuationAsync(id, request, ct);
        return ApiResponse.From(result, "Valuation recorded.");
    }

    [HttpPost("{id:guid}/valuations/initial/approve")]
    [Authorize(Policy = "InternalOnly")]
    public async Task<IActionResult> ApproveInitialValuation(Guid id, [FromBody] SemanticTransitionRequest request, CancellationToken ct)
    {
        var result = await service.ApproveInitialValuationAsync(id, request.Comment, ct);
        return ApiResponse.From(result, "Initial valuation approved.", HttpStatusCode.Accepted);
    }

    [HttpPost("{id:guid}/valuations/secondary/approve")]
    [Authorize(Policy = "InternalOnly")]
    public async Task<IActionResult> ApproveSecondaryValuation(Guid id, [FromBody] SemanticTransitionRequest request, CancellationToken ct)
    {
        var result = await service.ApproveSecondaryValuationAsync(id, request.Comment, ct);
        return ApiResponse.From(result, "Secondary valuation approved.", HttpStatusCode.Accepted);
    }

    #endregion

    #region Legal & Contracts

    [HttpPost("{id:guid}/contracts/preliminary/upload")]
    [Authorize(Policy = "InternalOnly")]
    public async Task<IActionResult> UploadPreliminaryContract(Guid id, [FromQuery] string s3Key, CancellationToken ct)
    {
        var result = await service.UploadPreliminaryContractAsync(id, s3Key, ct);
        return ApiResponse.From(result, "Preliminary contract uploaded.");
    }

    [HttpPost("{id:guid}/contracts/preliminary/approve")]
    [Authorize(Policy = "ApplicantOnly")]
    public async Task<IActionResult> ApprovePreliminaryContract(Guid id, [FromBody] SemanticTransitionRequest request, CancellationToken ct)
    {
        var result = await service.ApprovePreliminaryContractAsync(id, request.Comment, ct);
        return ApiResponse.From(result, "Preliminary contract approved.", HttpStatusCode.Accepted);
    }

    [HttpPost("{id:guid}/contracts/preliminary/revision-request")]
    [Authorize(Policy = "ApplicantOnly")]
    public async Task<IActionResult> RequestPreliminaryContractRevision(Guid id, [FromBody] SemanticRevisionRequest request, CancellationToken ct)
    {
        var result = await service.RequestPreliminaryContractRevisionAsync(id, request.Message, ct);
        return ApiResponse.From(result, "Preliminary contract revision requested.", HttpStatusCode.Accepted);
    }

    [HttpPost("{id:guid}/contracts/finalize-draft")]
    [Authorize(Policy = "InternalOnly")]
    public async Task<IActionResult> FinalizeContractDraft(Guid id, [FromBody] SemanticTransitionRequest request, CancellationToken ct)
    {
        var result = await service.FinalizeContractDraftAsync(id, request.Comment, ct);
        return ApiResponse.From(result, "Contract draft finalized.", HttpStatusCode.Accepted);
    }

    [HttpPost("{id:guid}/contracts/confirm-signature")]
    [Authorize(Policy = "InternalOnly")]
    public async Task<IActionResult> ConfirmSignature(Guid id, [FromBody] SemanticTransitionRequest request, CancellationToken ct)
    {
        var result = await service.ConfirmSignatureAsync(id, request.Comment, ct);
        return ApiResponse.From(result, "Contract signature confirmed.", HttpStatusCode.Accepted);
    }

    [HttpPost("{id:guid}/contracts/signed/upload")]
    [Authorize(Policy = "InternalOnly")]
    public async Task<IActionResult> UploadSignedContract(Guid id, [FromQuery] string s3Key, CancellationToken ct)
    {
        var result = await service.UploadSignedContractAsync(id, s3Key, ct);
        return ApiResponse.From(result, "Signed contract uploaded.");
    }

    #endregion

    #region Finance & Payments

    [HttpPut("{id:guid}/financial-worksheet")]
    [Authorize(Policy = "InternalOnly")]
    public async Task<IActionResult> UpdateFinancialWorksheet(Guid id, [FromBody] UpdateFinancialWorksheetRequest request, CancellationToken ct)
    {
        var result = await service.UpdateFinancialWorksheetAsync(id, request, ct);
        return ApiResponse.From(result, "Financial worksheet updated.");
    }

    [HttpPost("{id:guid}/financial-worksheet/submit")]
    [Authorize(Policy = "InternalOnly")]
    public async Task<IActionResult> SubmitFinancialWorksheet(Guid id, [FromBody] SemanticTransitionRequest request, CancellationToken ct)
    {
        var result = await service.SubmitFinancialWorksheetAsync(id, request.Comment, ct);
        return ApiResponse.From(result, "Financial worksheet submitted.", HttpStatusCode.Accepted);
    }

    [HttpPost("{id:guid}/financial-worksheet/approve")]
    [Authorize(Policy = "InternalOnly")]
    public async Task<IActionResult> ApproveFinancialWorksheet(Guid id, [FromBody] SemanticTransitionRequest request, CancellationToken ct)
    {
        var result = await service.ApproveFinancialWorksheetAsync(id, request.Comment, ct);
        return ApiResponse.From(result, "Financial worksheet approved.", HttpStatusCode.Accepted);
    }

    [HttpPost("{id:guid}/financial-worksheet/revision-request")]
    [Authorize(Policy = "InternalOnly")]
    public async Task<IActionResult> RequestFinancialWorksheetRevision(Guid id, [FromBody] SemanticRevisionRequest request, CancellationToken ct)
    {
        var result = await service.RequestFinancialWorksheetRevisionAsync(id, request.Message, ct);
        return ApiResponse.From(result, "Financial worksheet revision requested.", HttpStatusCode.Accepted);
    }

    [HttpPost("{id:guid}/payments")]
    [Authorize(Policy = "InternalOnly")]
    public async Task<IActionResult> RecordPayment(Guid id, [FromBody] RecordPaymentRequest request, CancellationToken ct)
    {
        var result = await service.RecordPaymentAsync(id, request, ct);
        return ApiResponse.From(result, "Payment recorded.");
    }

    [HttpPost("{id:guid}/payments/{paymentId:guid}/confirm")]
    [Authorize(Policy = "InternalOnly")]
    public async Task<IActionResult> ConfirmPayment(Guid id, Guid paymentId, CancellationToken ct)
    {
        var result = await service.ConfirmPaymentAsync(id, paymentId, ct);
        return ApiResponse.From(result, "Payment confirmed.");
    }

    [HttpPost("{id:guid}/payments/{paymentId:guid}/cancel")]
    [Authorize(Policy = "InternalOnly")]
    public async Task<IActionResult> CancelPayment(Guid id, Guid paymentId, CancellationToken ct)
    {
        var result = await service.CancelPaymentAsync(id, paymentId, ct);
        return ApiResponse.From(result, "Payment cancelled.");
    }

    #endregion

    #region Negative Actions

    [HttpPost("{id:guid}/reject")]
    [Authorize(Policy = "InternalOnly")]
    public async Task<IActionResult> Reject(Guid id, [FromBody] SemanticRejectRequest request, CancellationToken ct)
    {
        var result = await service.RejectAsync(id, request.Reason, ct);
        return ApiResponse.From(result, "Investment case rejected.", HttpStatusCode.Accepted);
    }

    [HttpPost("{id:guid}/cancel")]
    [Authorize]
    public async Task<IActionResult> Cancel(Guid id, [FromBody] SemanticCancelRequest request, CancellationToken ct)
    {
        var result = await service.CancelAsync(id, request.Reason, ct);
        return ApiResponse.From(result, "Investment case cancelled.", HttpStatusCode.Accepted);
    }

    [HttpPost("{id:guid}/archive")]
    [Authorize]
    public async Task<IActionResult> Archive(Guid id, [FromBody] SemanticArchiveRequest request, CancellationToken ct)
    {
        var result = await service.ArchiveAsync(id, request.Reason, ct);
        return ApiResponse.From(result, "Investment case archived.", HttpStatusCode.Accepted);
    }

    #endregion

    #region Documents & Evaluations

    [HttpPost("{id:guid}/documents/presign")]
    [Authorize]
    public async Task<IActionResult> Presign(Guid id, [FromBody] PresignUploadRequest request, CancellationToken ct)
    {
        var result = await service.PresignDocumentUploadAsync(id, request, ct);
        return ApiResponse.From(result, "Document upload presigned.");
    }

    [HttpPost("{id:guid}/documents/confirm")]
    [Authorize]
    public async Task<IActionResult> Confirm(Guid id, [FromQuery] string s3Key, CancellationToken ct)
    {
        var result = await service.ConfirmDocumentUploadedAsync(id, s3Key, ct);
        return ApiResponse.From(result, "Document upload confirmed.");
    }

    [HttpGet("{id:guid}/documents")]
    [Authorize]
    public async Task<IActionResult> GetDocuments(Guid id, CancellationToken ct)
    {
        var result = await service.GetDocumentsAsync(id, ct);
        return ApiResponse.From(result, "Documents retrieved.");
    }

    [HttpGet("{id:guid}/documents/{documentId:guid}/download")]
    [Authorize]
    public async Task<IActionResult> DownloadDocument(Guid id, Guid documentId, CancellationToken ct)
    {
        var result = await service.PresignDocumentDownloadAsync(id, documentId, ct);
        return ApiResponse.From(result, "Document download presigned.");
    }

    [HttpPost("{id:guid}/evaluations")]
    [Authorize(Policy = "InternalOnly")]
    public async Task<IActionResult> UpsertEvaluation(Guid id, [FromBody] CaseEvaluationUpsertRequest request, CancellationToken ct)
    {
        var result = await service.UpsertEvaluationAsync(id, request, ct);
        return ApiResponse.From(result, "Evaluation saved.");
    }

    [HttpGet("{id:guid}/evaluations")]
    [Authorize]
    public async Task<IActionResult> GetEvaluations(Guid id, CancellationToken ct)
    {
        var result = await service.GetEvaluationsAsync(id, ct);
        return ApiResponse.From(result, "Evaluations retrieved.");
    }

    #endregion

    #region Comments

    [HttpGet("{id:guid}/comments")]
    [Authorize]
    public async Task<IActionResult> GetComments(Guid id, [FromQuery] bool includeInternal = false, CancellationToken ct = default)
    {
        var result = await service.GetCommentsAsync(id, includeInternal, ct);
        return ApiResponse.From(result, "Comments retrieved.");
    }

    [HttpPost("{id:guid}/comments")]
    [Authorize]
    public async Task<IActionResult> AddComment(Guid id, [FromBody] AddCommentRequest request, CancellationToken ct)
    {
        var result = await service.AddCommentAsync(id, request, ct);
        return ApiResponse.From(result, "Comment added.");
    }

    [HttpPost("{id:guid}/comments/{commentId:guid}/attachments")]
    [Authorize]
    public async Task<IActionResult> AddCommentAttachment(Guid id, Guid commentId, [FromQuery] string s3Key, [FromQuery] string fileName, CancellationToken ct)
    {
        var result = await service.AddCommentAttachmentAsync(id, commentId, s3Key, fileName, ct);
        return ApiResponse.From(result, "Comment attachment added.");
    }

    #endregion
}
