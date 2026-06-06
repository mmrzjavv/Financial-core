# Loan Case API Guide

Base path: `POST/GET /api/v1/loancases`

## Workflow (status codes)

| Status | Name |
|--------|------|
| 1 | Draft |
| 2 | DataEntry |
| 3 | PendingCreditReview |
| 4 | RevisionRequestedByCredit |
| 5 | PendingCeoInitialApproval |
| 6 | CanceledByCeo |
| 7 | PendingLegalRawContract |
| 8 | PendingApplicantSignature |
| 9 | PendingLegalFinalReview |
| 10 | RevisionRequestedByLegal |
| 11 | PendingFinancialReview |
| 12 | RevisionRequestedByFinancial |
| 13 | PendingLegalFinalContract |
| 14 | PendingCeoFinalApproval |
| 15 | ReadyForPayment |
| 16 | RepaymentPhase |
| 17 | Completed |
| 18 | Archived |

## Key endpoints

- `POST /` — create case (Applicant)
- `PUT /{id}/application` — Data Entry 1
- `POST /{id}/application/submit` — submit to credit review
- `PUT /{id}/approval-detail` — credit approval form
- `POST /{id}/credit/approve` | `/credit/revision-request`
- `POST /{id}/ceo/initial/approve` | `/ceo/initial/reject`
- `PUT /{id}/installments` — bulk upsert schedule
- `POST /{id}/legal/setup-complete` — raw contract + installments done
- `POST /{id}/signed-package/submit` — applicant signed contract
- `POST /{id}/legal/approve` | `/legal/revision-request`
- `POST /{id}/financial/approve` | `/financial/revision-request`
- `POST /{id}/legal/final-uploaded` — after final contract document confirm
- `POST /{id}/ceo/final/approve` | `/ceo/final/reject`
- `POST /{id}/payments` — register payout
- `GET /{id}/installments` — applicant repayment dashboard
- Documents: `POST /{id}/documents/presign` → upload → `POST /{id}/documents/confirm?s3Key=...`

## Configuration

`appsettings.json`:

```json
"LoanSettings": {
  "InstallmentReminderDays": 7,
  "ReminderPollIntervalMinutes": 60
}
```

Background service sends SMS via `SmsTemplateId.LoanInstallmentDueReminder` when installment due date is exactly N days away.
