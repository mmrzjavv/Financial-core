from __future__ import annotations

from typing import Any

import httpx

from e2e_simulator.config import SimulatorConfig
from e2e_simulator.models import ApiErrorRecord


class ApiClientError(Exception):
    def __init__(
        self,
        message: str,
        *,
        status_code: int | None = None,
        validation_errors: list[str] | None = None,
    ) -> None:
        super().__init__(message)
        self.status_code = status_code
        self.validation_errors = validation_errors or []


class ApiClient:
    def __init__(self, config: SimulatorConfig) -> None:
        self.config = config
        self._client: httpx.AsyncClient | None = None

    async def __aenter__(self) -> ApiClient:
        self._client = httpx.AsyncClient(
            base_url=self.config.base_url,
            timeout=httpx.Timeout(self.config.request_timeout_seconds),
            headers={"Accept": "application/json"},
        )
        return self

    async def __aexit__(self, *args: object) -> None:
        if self._client:
            await self._client.aclose()
            self._client = None

    @property
    def client(self) -> httpx.AsyncClient:
        if self._client is None:
            raise RuntimeError("ApiClient must be used as an async context manager.")
        return self._client

    def url(self, path: str) -> str:
        if path.startswith("http://") or path.startswith("https://"):
            return path
        if not path.startswith("/"):
            path = f"/{path}"
        return path

    @staticmethod
    def unwrap(body: Any) -> Any:
        if isinstance(body, dict) and "success" in body:
            if body.get("success") is False:
                errors = body.get("validationErrors") or body.get("validation_errors") or []
                if isinstance(errors, list):
                    detail = "; ".join(str(e) for e in errors) if errors else str(body.get("message", "API error"))
                else:
                    detail = str(body.get("message", "API error"))
                raise ApiClientError(detail, validation_errors=errors if isinstance(errors, list) else None)
            data = body.get("data")
            if data is not None:
                return data
            if "list" in body and body.get("list") is not None:
                return body.get("list")
            return body.get("data")
        return body

    @staticmethod
    def pick_id(payload: Any) -> str | None:
        if not isinstance(payload, dict):
            return None
        for key in ("id", "Id", "caseId", "CaseId"):
            value = payload.get(key)
            if value:
                return str(value)
        return None

    async def request(
        self,
        method: str,
        path: str,
        *,
        token: str | None = None,
        json_body: Any | None = None,
        use_auth: bool = True,
        expect_json: bool = True,
    ) -> Any:
        headers: dict[str, str] = {}
        if use_auth and token:
            headers["Authorization"] = f"Bearer {token}"
        if json_body is not None:
            headers["Content-Type"] = "application/json"

        response = await self.client.request(
            method.upper(),
            self.url(path),
            headers=headers,
            json=json_body if json_body is not None else None,
        )

        if response.status_code >= 400:
            detail = response.text
            try:
                parsed = response.json()
                if isinstance(parsed, dict):
                    detail = str(parsed.get("message") or parsed)
            except Exception:
                pass
            raise ApiClientError(detail, status_code=response.status_code)

        if not expect_json or response.status_code == 204:
            return None

        if not response.content:
            return None

        body = response.json()
        return self.unwrap(body)

    async def upload_bytes(self, url: str, content: bytes, mime_type: str) -> None:
        response = await self.client.put(
            url,
            content=content,
            headers={"Content-Type": mime_type},
        )
        if response.status_code >= 400:
            raise ApiClientError(
                f"Storage upload failed with status {response.status_code}",
                status_code=response.status_code,
            )

    def record_error(
        self,
        *,
        module: str,
        case_index: int,
        step: str,
        method: str,
        path: str,
        exc: Exception,
    ) -> ApiErrorRecord:
        status_code = exc.status_code if isinstance(exc, ApiClientError) else None
        return ApiErrorRecord(
            module=module,
            case_index=case_index,
            step=step,
            method=method,
            path=path,
            status_code=status_code,
            message=str(exc),
        )
