from __future__ import annotations

from abc import ABC, abstractmethod
from typing import Callable

from e2e_simulator.api_client import ApiClient, ApiClientError
from e2e_simulator.auth import AuthManager
from e2e_simulator.config import SimulatorConfig
from e2e_simulator.models import ApiErrorRecord, CaseRunResult, SimulationPath


LogFn = Callable[[str], None]


class WorkflowRunner(ABC):
    module_name: str
    base_path: str

    def __init__(
        self,
        api: ApiClient,
        auth: AuthManager,
        config: SimulatorConfig,
        log: LogFn | None = None,
    ) -> None:
        self.api = api
        self.auth = auth
        self.config = config
        self.log = log or (lambda _msg: None)
        self.steps = 0
        self.api_errors: list[ApiErrorRecord] = []

    @abstractmethod
    async def run_case(self, case_index: int, path: SimulationPath) -> CaseRunResult:
        raise NotImplementedError

    async def _step(self, label: str, coro_factory: Callable[[], object]):
        self.log(f"[{self.module_name}#{self.steps + 1}] {label}")
        result = await coro_factory()
        self.steps += 1
        return result

    async def _call(
        self,
        *,
        case_index: int,
        step: str,
        method: str,
        path: str,
        token: str | None = None,
        json_body: object | None = None,
        use_auth: bool = True,
    ):
        try:
            return await self.api.request(
                method,
                path,
                token=token,
                json_body=json_body,
                use_auth=use_auth,
            )
        except ApiClientError as exc:
            self.api_errors.append(
                self.api.record_error(
                    module=self.module_name,
                    case_index=case_index,
                    step=step,
                    method=method,
                    path=path,
                    exc=exc,
                )
            )
            raise

    async def _get_case_snapshot(self, case_id: str, token: str) -> dict:
        payload = await self._call(
            case_index=-1,
            step="get-case",
            method="GET",
            path=f"{self.base_path}/{case_id}",
            token=token,
        )
        return payload if isinstance(payload, dict) else {}

    def _status_name(self, payload: dict) -> str:
        status = payload.get("currentStatus") or payload.get("CurrentStatus")
        if isinstance(status, dict):
            return str(status.get("name") or status.get("Name") or status)
        if status is not None:
            return str(status)
        key = payload.get("statusKey") or payload.get("StatusKey")
        return str(key) if key else "unknown"

    def _phase_name(self, payload: dict) -> str | None:
        phase = payload.get("currentPhase") or payload.get("CurrentPhase")
        if phase is None:
            return None
        if isinstance(phase, dict):
            return str(phase.get("name") or phase.get("Name") or phase)
        return str(phase)

    async def _maybe_enrich(self, case_id: str, case_index: int, path: SimulationPath) -> None:
        if path != SimulationPath.ENRICHMENT:
            return
        await self._inject_enrichment(case_id, case_index)

    async def _inject_enrichment(self, case_id: str, case_index: int) -> None:
        """Override in modules that support comments or extra uploads."""
        return
