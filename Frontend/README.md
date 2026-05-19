# Backend Workflow Test Panel (Vanilla)

This folder contains a **simple, non-production** frontend used to validate the **full end-to-end business workflow** of the Maskan .NET microservice platform **through the API Gateway**.

## Files

- `index.html` – UI layout
- `app.js` – logic (fetch + localStorage + debug inspector)
- `styles.css` – minimal styling
- `config.js` – base URL + version config (editable in UI)

## Run

You should serve this folder over HTTP (not `file://`) to avoid browser CORS/security issues.

Example (PowerShell):

```powershell
cd d:\work\Maskan\Panel\Core
python -m http.server 5500
```

Then open:

```text
http://localhost:5500/index.html
```

If you don’t have Python, use any static file server you already have (IIS, nginx, etc.).

## Configure

Top bar → **Edit**:

- **API Gateway Base URL**: default `http://localhost:5141`
- **Cases API version segment**: default `1` (routes are `/api/v{version}/investmentcases`)

Config is stored in `localStorage`.

## What’s Covered

This panel wires to the existing gateway/service endpoints and supports:

- **Auth**
  - Send OTP: `POST /api/v1/identity/users/send-otp`
  - Verify OTP: `POST /api/v1/identity/users/verify-otp`
  - Refresh token: `POST /api/v1/identity/users/refresh-token`
  - Logout + sessions + profile
- **Users**
  - Create user: `POST /api/v1/identity/users`
  - Update user (role/active): `PUT /api/v1/identity/users/{id}`
  - Get/list users
- **Investment cases**
  - Create / get / search / history: `/api/v{v}/investmentcases...`
  - DataEntry1 / DataEntry2: update, submit, approve, revision-request
  - Valuation: record + initial/secondary approvals
  - Contracts: preliminary upload, applicant approve/revision, finalize draft, confirm signature, signed upload
  - Finance: worksheet update/submit/approve/revision + payments record/confirm/cancel
  - Negative actions: reject/cancel/archive
- **Documents**
  - Presign upload → PUT to URL → confirm upload
  - List documents
  - Presign download
  - Generic presigned PUT + generic multipart POST utility
- **Comments & Collaboration**
  - Get comments (+ includeInternal)
  - Add comment
  - Attach comment file via `s3Key` + `fileName`
- **Evaluations**
  - Upsert evaluation
  - Get evaluations
- **Debug Tools**
  - Manual request sender (any method/path)
  - Request/response inspector (status, headers, timing)
  - Request log
  - Token viewer + clear localStorage

## Role Switching

The panel supports **testing multiple roles** by saving multiple OTP logins as **sessions** in `localStorage`.

Auth → **Saved Sessions** → **Use** (switches which Bearer token is attached to requests).

Note: The Identity service token is **encrypted JWT (JWE)**, so the panel does not decode claims client-side; it displays role/user info from the login/profile responses.

## Notes / Gotchas

- Most workflow endpoints require the correct role/permissions. If a button returns `403`, switch to a different session/role.
- Presigned uploads depend on your storage/CORS config (S3/MinIO). If browser PUT is blocked, you can still validate the presign/confirm flow by uploading via another tool and then confirming with this panel.

