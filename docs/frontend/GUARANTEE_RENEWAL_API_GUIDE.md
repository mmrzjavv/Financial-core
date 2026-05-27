# Guarantee Renewal API Guide

Base path: `/api/v1/guarantee-renewals`

## Flow

1. **Draft** — Applicant creates renewal from a **Completed** parent guarantee case.
2. **CeoReview** — `POST /{id}/submit` then CEO `approve` or `reject`.
3. **CreditDateUpdate** — Credit unit `PUT /{id}/credit/dates` with `approvedExpiryDate` → **Completed**.

## Endpoints

| Method | Path | Role |
|--------|------|------|
| POST | `/api/v1/guarantee-renewals` | Applicant |
| GET | `/api/v1/guarantee-renewals/{id}` | Authenticated |
| POST | `/api/v1/guarantee-renewals/{id}/submit` | Applicant |
| POST | `/api/v1/guarantee-renewals/{id}/ceo/approve` | CEO |
| POST | `/api/v1/guarantee-renewals/{id}/ceo/reject` | CEO |
| PUT | `/api/v1/guarantee-renewals/{id}/credit/dates` | CreditExpert/Manager |

## Create body

```json
{
  "parentGuaranteeCaseId": "guid",
  "renewalKind": 1,
  "requestedExpiryDate": "2027-06-01",
  "requestedAmount": null
}
```

`renewalKind`: `1` Extension, `2` Reduction

## Credit dates body

```json
{ "approvedExpiryDate": "2027-06-01" }
```
