using Core.Domain.Enums;

namespace Core.Application.Notifications.Sms;

public interface ICaseWorkflowSmsNotifier
{
    Task NotifyStatusChangeAsync(
        Guid caseId,
        string applicantUserId,
        string caseNumber,
        CaseStatus from,
        CaseStatus to,
        WorkflowAction action,
        CancellationToken cancellationToken = default);
}
