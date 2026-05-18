using Core.Application.Abstractions;
using Core.Application.Kanban;
using Core.Application.Logging;
using Core.Application.Notifications.Sms;
using Core.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Core.Infrastructure.Notifications.Sms;

public sealed class CaseWorkflowSmsNotifier(
    ICoreDbContext dbContext,
    ISmsDispatcher smsDispatcher,
    ILogger<CaseWorkflowSmsNotifier> logger) : ICaseWorkflowSmsNotifier
{
    public async Task NotifyStatusChangeAsync(
        Guid caseId,
        string applicantUserId,
        string caseNumber,
        CaseStatus from,
        CaseStatus to,
        WorkflowAction action,
        CancellationToken cancellationToken = default)
    {
        if (from == to)
            return;

        if (string.IsNullOrWhiteSpace(caseNumber))
        {
            logger.LogDebug("Skipping workflow SMS for case {CaseId}: case number missing", caseId);
            return;
        }

        var mobile = await ResolveApplicantMobileAsync(applicantUserId, cancellationToken);
        if (string.IsNullOrWhiteSpace(mobile))
        {
            logger.LogDebug("Skipping workflow SMS for case {CaseId}: applicant mobile not found", caseId);
            return;
        }

        var statusTitle = CaseKanbanRules.GetStatusTitle(to);
        var args = new Dictionary<string, string>
        {
            ["caseNumber"] = caseNumber,
            ["statusTitle"] = statusTitle
        };

        var template = ResolveTemplate(from, to, action);
        await smsDispatcher.EnqueueAsync(template, mobile, args, cancellationToken: cancellationToken);

        ApplicationLog.Completed(logger,
            "Workflow SMS queued for case {CaseId} ({CaseNumber}) — template {TemplateId}, {FromStatus} → {ToStatus}, action {Action}",
            caseId, caseNumber, template, from, to, action);
    }

    private static SmsTemplateId ResolveTemplate(CaseStatus from, CaseStatus to, WorkflowAction action)
    {
        if (action == WorkflowAction.Submit)
        {
            if (to == CaseStatus.ReviewDataEntry1)
                return SmsTemplateId.De1Submitted;
            if (to == CaseStatus.ReviewDataEntry2)
                return SmsTemplateId.De2Submitted;
        }

        if (action == WorkflowAction.Reject || to == CaseStatus.Rejected)
        {
            if (from is CaseStatus.ReviewDataEntry1 or CaseStatus.DataEntry1)
                return SmsTemplateId.De1Rejected;
            if (from is CaseStatus.ReviewDataEntry2 or CaseStatus.DataEntry2)
                return SmsTemplateId.De2Rejected;
            return SmsTemplateId.CaseRejected;
        }

        if (action == WorkflowAction.RequestRevision)
        {
            if (from == CaseStatus.ReviewDataEntry1 || to == CaseStatus.DataEntry1)
                return SmsTemplateId.De1Revision;
            if (from == CaseStatus.ReviewDataEntry2 || to == CaseStatus.DataEntry2)
                return SmsTemplateId.De2Revision;
        }

        if (action == WorkflowAction.Approve)
        {
            if (from == CaseStatus.ReviewDataEntry1 && to == CaseStatus.DataEntry2)
                return SmsTemplateId.De1Approved;
            if (from == CaseStatus.ReviewDataEntry2 && to is CaseStatus.InitialValuation or CaseStatus.SecondaryValuation)
                return SmsTemplateId.De2Approved;
            if (to is CaseStatus.WaitingPreliminaryContract or CaseStatus.InitialValuation &&
                from is CaseStatus.SecondaryValuation or CaseStatus.ReviewDataEntry2)
                return SmsTemplateId.ValuationApproved;
            if (to == CaseStatus.WaitingCeoApproval)
                return SmsTemplateId.FinancialWorksheetApproved;
            if (from == CaseStatus.WaitingCeoApproval && to == CaseStatus.WaitingPayment)
                return SmsTemplateId.CeoApproved;
        }

        if (to == CaseStatus.Completed)
            return SmsTemplateId.CaseCompleted;

        if (to == CaseStatus.WaitingPayment && from == CaseStatus.FinancialWorksheetReview)
            return SmsTemplateId.PaymentRecorded;

        if (to == CaseStatus.WaitingUserReviewPreliminaryContract)
            return SmsTemplateId.ContractReady;

        return SmsTemplateId.CaseStatusChanged;
    }

    private async Task<string?> ResolveApplicantMobileAsync(string applicantUserId, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(applicantUserId, out var userId))
            return null;

        return await dbContext.Users
            .AsNoTracking()
            .Where(u => u.Id == userId && u.IsActive)
            .Select(u => u.PhoneNumber)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
