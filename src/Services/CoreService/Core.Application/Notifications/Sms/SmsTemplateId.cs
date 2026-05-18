namespace Core.Application.Notifications.Sms;

/// <summary>Workflow SMS templates (100+). OTP uses <see cref="Identity.Interfaces.ISmsService.SendOtpAsync"/>.</summary>
public enum SmsTemplateId
{
    CaseStatusChanged = 100,
    De1Submitted = 1001,
    De1Approved = 101,
    De1Revision = 102,
    De1Rejected = 103,
    De2Submitted = 1002,
    De2Approved = 104,
    De2Revision = 105,
    De2Rejected = 106,
    ValuationApproved = 107,
    ContractReady = 108,
    FinancialWorksheetApproved = 109,
    CeoApproved = 110,
    PaymentRecorded = 111,
    CaseRejected = 112,
    CaseCompleted = 113
}
