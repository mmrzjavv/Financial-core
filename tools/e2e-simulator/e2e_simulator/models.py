from __future__ import annotations

from dataclasses import dataclass, field
from datetime import datetime, timezone
from enum import Enum
from typing import Any


class SimulationModule(str, Enum):
    INVESTMENT = "investment"
    GUARANTEE = "guarantee"
    LOAN = "loan"


class SimulationPath(str, Enum):
    HAPPY = "happy"
    REVISION = "revision"
    REJECTION = "rejection"
    ENRICHMENT = "enrichment"


@dataclass
class ApiErrorRecord:
    module: str
    case_index: int
    step: str
    method: str
    path: str
    status_code: int | None
    message: str


@dataclass
class CaseRunResult:
    module: str
    case_index: int
    path: SimulationPath
    case_id: str | None = None
    case_number: str | None = None
    success: bool = False
    final_status: str | None = None
    final_phase: str | None = None
    steps_taken: int = 0
    error: str | None = None
    api_errors: list[ApiErrorRecord] = field(default_factory=list)

    def to_dict(self) -> dict[str, Any]:
        return {
            "module": self.module,
            "case_index": self.case_index,
            "path": self.path.value,
            "case_id": self.case_id,
            "case_number": self.case_number,
            "success": self.success,
            "final_status": self.final_status,
            "final_phase": self.final_phase,
            "steps_taken": self.steps_taken,
            "error": self.error,
            "api_errors": [e.__dict__ for e in self.api_errors],
        }


@dataclass
class BatchReport:
    run_id: str
    started_at: datetime
    finished_at: datetime | None = None
    modules: list[str] = field(default_factory=list)
    cases_attempted: int = 0
    cases_succeeded: int = 0
    cases_failed: int = 0
    path_breakdown: dict[str, int] = field(default_factory=dict)
    status_breakdown: dict[str, int] = field(default_factory=dict)
    avg_steps_by_path: dict[str, float] = field(default_factory=dict)
    case_results: list[CaseRunResult] = field(default_factory=list)
    api_errors: list[ApiErrorRecord] = field(default_factory=list)
    logs: list[str] = field(default_factory=list)

    @staticmethod
    def new_run(modules: list[str]) -> BatchReport:
        return BatchReport(
            run_id=datetime.now(timezone.utc).strftime("%Y%m%dT%H%M%SZ"),
            started_at=datetime.now(timezone.utc),
            modules=modules,
        )

    def finalize(self) -> None:
        self.finished_at = datetime.now(timezone.utc)
        self.cases_attempted = len(self.case_results)
        self.cases_succeeded = sum(1 for r in self.case_results if r.success)
        self.cases_failed = self.cases_attempted - self.cases_succeeded

        self.path_breakdown = {}
        self.status_breakdown = {}
        steps_by_path: dict[str, list[int]] = {}

        for result in self.case_results:
            self.path_breakdown[result.path.value] = self.path_breakdown.get(result.path.value, 0) + 1
            status_key = result.final_status or "unknown"
            self.status_breakdown[status_key] = self.status_breakdown.get(status_key, 0) + 1
            steps_by_path.setdefault(result.path.value, []).append(result.steps_taken)
            self.api_errors.extend(result.api_errors)

        self.avg_steps_by_path = {
            path: (sum(values) / len(values) if values else 0.0)
            for path, values in steps_by_path.items()
        }

    def to_dict(self) -> dict[str, Any]:
        return {
            "run_id": self.run_id,
            "started_at": self.started_at.isoformat(),
            "finished_at": self.finished_at.isoformat() if self.finished_at else None,
            "modules": self.modules,
            "cases_attempted": self.cases_attempted,
            "cases_succeeded": self.cases_succeeded,
            "cases_failed": self.cases_failed,
            "path_breakdown": self.path_breakdown,
            "status_breakdown": self.status_breakdown,
            "avg_steps_by_path": self.avg_steps_by_path,
            "case_results": [r.to_dict() for r in self.case_results],
            "api_errors": [e.__dict__ for e in self.api_errors],
        }
