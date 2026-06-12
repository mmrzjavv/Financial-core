from __future__ import annotations

from pathlib import Path

from e2e_simulator.api_client import ApiClient

MINIMAL_PDF = b"%PDF-1.4\n% E2E simulator test document\n1 0 obj<<>>endobj\ntrailer<<>>\n%%EOF\n"
MIME_PDF = "application/pdf"


def sample_pdf_bytes() -> bytes:
    asset = Path(__file__).resolve().parents[1] / "assets" / "sample.pdf"
    if asset.is_file():
        return asset.read_bytes()
    return MINIMAL_PDF


async def upload_case_document(
    api: ApiClient,
    *,
    base_path: str,
    case_id: str,
    token: str,
    document_type: int,
    file_name: str,
    content: bytes | None = None,
    mime_type: str = MIME_PDF,
) -> str:
    payload = content if content is not None else sample_pdf_bytes()
    presign = await api.request(
        "POST",
        f"{base_path}/{case_id}/documents/presign",
        token=token,
        json_body={
            "documentType": document_type,
            "fileName": file_name,
            "mimeType": mime_type,
            "fileSize": len(payload),
        },
    )
    upload_url = presign.get("url") or presign.get("Url")
    s3_key = presign.get("s3Key") or presign.get("S3Key")
    if not upload_url or not s3_key:
        raise ValueError("Presign response missing url or s3Key")

    await api.upload_bytes(upload_url, payload, mime_type)
    await api.request(
        "POST",
        f"{base_path}/{case_id}/documents/confirm?s3Key={s3_key}",
        token=token,
        json_body=None,
        expect_json=True,
    )
    return str(s3_key)
