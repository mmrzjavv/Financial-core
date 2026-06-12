from __future__ import annotations

from dataclasses import dataclass, field
from pathlib import Path
from typing import Any

import yaml


@dataclass
class Persona:
    key: str
    phone: str
    role: int
    first_name: str
    last_name: str


@dataclass
class PathWeights:
    happy: float = 0.40
    revision: float = 0.40
    rejection: float = 0.10
    enrichment: float = 0.10

    def as_dict(self) -> dict[str, float]:
        return {
            "happy": self.happy,
            "revision": self.revision,
            "rejection": self.rejection,
            "enrichment": self.enrichment,
        }


@dataclass
class SimulatorConfig:
    base_url: str = "http://localhost:5081"
    api_version: str = "1"
    dev_otp: str = "123456"
    seed_admin_phone: str = ""
    max_concurrent_cases: int = 5
    request_timeout_seconds: float = 120.0
    default_cases_per_module: int = 100
    path_weights: PathWeights = field(default_factory=PathWeights)
    personas: list[Persona] = field(default_factory=list)
    reports_dir: str = "reports"

    @property
    def api_prefix(self) -> str:
        return f"/api/v{self.api_version}"

    @property
    def reports_path(self) -> Path:
        return Path(self.reports_dir)


def _persona_from_dict(data: dict[str, Any]) -> Persona:
    return Persona(
        key=str(data["key"]),
        phone=str(data["phone"]),
        role=int(data["role"]),
        first_name=str(data.get("first_name", data.get("firstName", "E2E"))),
        last_name=str(data.get("last_name", data.get("lastName", "User"))),
    )


def load_config(path: str | Path | None = None) -> SimulatorConfig:
    root = Path(__file__).resolve().parents[1]
    config_path = Path(path) if path else root / "config.yaml"
    if not config_path.is_file():
        example = root / "config.example.yaml"
        if example.is_file():
            config_path = example
        else:
            return SimulatorConfig()

    with config_path.open(encoding="utf-8") as handle:
        raw = yaml.safe_load(handle) or {}

    weights_raw = raw.get("path_weights") or {}
    weights = PathWeights(
        happy=float(weights_raw.get("happy", 0.40)),
        revision=float(weights_raw.get("revision", 0.40)),
        rejection=float(weights_raw.get("rejection", 0.10)),
        enrichment=float(weights_raw.get("enrichment", 0.10)),
    )

    personas = [_persona_from_dict(item) for item in raw.get("personas") or []]

    return SimulatorConfig(
        base_url=str(raw.get("base_url", "http://localhost:5081")).rstrip("/"),
        api_version=str(raw.get("api_version", "1")),
        dev_otp=str(raw.get("dev_otp", "123456")),
        seed_admin_phone=str(raw.get("seed_admin_phone", "") or ""),
        max_concurrent_cases=int(raw.get("max_concurrent_cases", 5)),
        request_timeout_seconds=float(raw.get("request_timeout_seconds", 120)),
        default_cases_per_module=int(raw.get("default_cases_per_module", 100)),
        path_weights=weights,
        personas=personas,
        reports_dir=str(raw.get("reports_dir", "reports")),
    )
