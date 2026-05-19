namespace Core.API.Swagger;

internal static class OpenApiMetadata
{
    public const string Title = "Financial Core API";
    public const string Version = "v1";

    public const string ContactName = "Mohammadreza Javaheri";
    public const string ContactEmail = "mohammad.r.javaheri@gmail.com";
    public const string ContactPhone = "09358357344";

    public const string Description = """
        ## Overview

        **Financial-Core** is the backend API for a **financial fund operations platform**. The system is designed to grow into a full panel that digitizes fund workflows end to end—not only investment, but the broader operational processes the fund runs through this application.

        ### Current modules (v1)

        - **Identity & users** — OTP login, JWT, sessions, roles and permissions
        - **Investment cases** — multi-stage applications, workflow, reviews, valuations, contracts, financial worksheet, CEO approval, payments
        - **Documents** — client-side upload (presign → PUT to object storage → confirm)
        - **Kanban** — action-required and watching work queues per role
        - **Executive dashboards** — CEO and board metrics
        - **Companies** — applicant company profiles

        Additional fund operations modules will be added to this API over time; treat investment cases as one domain area within the larger platform.

        ### Technical notes

        - **Base path:** `/api/v1.0`
        - **Responses:** `ApiOperationResult<T>` (`success`, `message`, `data`, …)
        - **Auth:** `Authorization: Bearer {JWT}` (from `POST /api/v1.0/panel/users/verify-otp`)
        - **File uploads:** upload bytes from the browser to the presigned URL; then call confirm—do not stream large files through Core.API in production

        ### Contact

        Mohammadreza Javaheri — [mohammad.r.javaheri@gmail.com](mailto:mohammad.r.javaheri@gmail.com) — +98 935 835 7344
        """;
}
