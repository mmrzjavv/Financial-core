from __future__ import annotations

import asyncio
import csv
import json
import random
from pathlib import Path
from typing import Callable

from e2e_simulator.api_client import ApiClient
from e2e_simulator.auth import AuthManager
from e2e_simulator.config import SimulatorConfig
from e2e_simulator.models import BatchReport, SimulationModule, SimulationPath
from e2e_simulator.workflows.guarantee import GuaranteeWorkflow
from e2e_simulator.workflows.investment import InvestmentWorkflow
from e2e_simulator.workflows.loan import LoanWorkflow

LogFn = Callable[[str], None]
ProgressFn = Callable[[float, str], None]

WORKFLOW_MAP = {
    SimulationModule.INVESTMENT: InvestmentWorkflow,
    SimulationModule.GUARANTEE: GuaranteeWorkflow,
    SimulationModule.LOAN: LoanWorkflow,
}


def pick_path(config: SimulatorConfig, rng: random.Random | None = None) -> SimulationPath:
    rng = rng or random.Random()
    weights = config.path_weights.as_dict()
    paths = list(weights.keys())
    values = [weights[p] for p in paths]
    choice = rng.choices(paths, weights=values, k=1)[0]
    return SimulationPath(choice)


class SimulationOrchestrator:
    def __init__(
        self,
        config: SimulatorConfig,
        log: LogFn | None = None,
        progress: ProgressFn | None = None,
        stop_event: asyncio.Event | None = None,
    ) -> None:
        self.config = config
        self.log = log or (lambda _msg: None)
        self.progress = progress or (lambda _pct, _msg: None)
        self.stop_event = stop_event or asyncio.Event()

    async def run_modules(
        self,
        modules: list[SimulationModule],
        cases_per_module: int,
        *,
        enabled_paths: dict[str, bool] | None = None,
    ) -> BatchReport:
        report = BatchReport.new_run([m.value for m in modules])
        self.config.reports_path.mkdir(parents=True, exist_ok=True)

        async with ApiClient(self.config) as api:
            auth = AuthManager(api, self.config)
            self.log("Provisioning E2E personas and roles…")
            await auth.ensure_users_provisioned(log=self.log)

            total_cases = len(modules) * cases_per_module
            completed = 0

            for module in modules:
                if self.stop_event.is_set():
                    self.log("Stop requested — halting batch.")
                    break

                self.log(f"=== Starting module: {module.value} ({cases_per_module} cases) ===")
                runner_cls = WORKFLOW_MAP[module]
                semaphore = asyncio.Semaphore(self.config.max_concurrent_cases)

                async def run_one(index: int) -> None:
                    nonlocal completed
                    if self.stop_event.is_set():
                        return
                    async with semaphore:
                        path = self._pick_path_for_run(enabled_paths)
                        runner = runner_cls(api, auth, self.config, log=self.log)
                        self.log(f"[{module.value}] case {index + 1}/{cases_per_module} path={path.value}")
                        result = await runner.run_case(index + 1, path)
                        report.case_results.append(result)
                        completed += 1
                        pct = completed / max(total_cases, 1)
                        self.progress(
                            pct,
                            f"{module.value}: case {index + 1}/{cases_per_module} "
                            f"({'ok' if result.success else 'fail'})",
                        )

                tasks = [asyncio.create_task(run_one(i)) for i in range(cases_per_module)]
                await asyncio.gather(*tasks, return_exceptions=False)

        report.finalize()
        self.progress(1.0, "Batch complete")
        return report

    def _pick_path_for_run(self, enabled_paths: dict[str, bool] | None) -> SimulationPath:
        weights = self.config.path_weights.as_dict()
        if enabled_paths:
            weights = {k: v for k, v in weights.items() if enabled_paths.get(k, True)}
        if not weights:
            return SimulationPath.HAPPY
        paths = list(weights.keys())
        values = [weights[p] for p in paths]
        choice = random.choices(paths, weights=values, k=1)[0]
        return SimulationPath(choice)


def save_report(report: BatchReport, reports_dir: Path) -> tuple[Path, Path]:
    reports_dir.mkdir(parents=True, exist_ok=True)
    json_path = reports_dir / f"e2e_report_{report.run_id}.json"
    csv_path = reports_dir / f"e2e_report_{report.run_id}.csv"

    with json_path.open("w", encoding="utf-8") as handle:
        json.dump(report.to_dict(), handle, ensure_ascii=False, indent=2)

    fieldnames = [
        "module",
        "case_index",
        "path",
        "case_id",
        "case_number",
        "success",
        "final_status",
        "final_phase",
        "steps_taken",
        "error",
    ]
    with csv_path.open("w", encoding="utf-8", newline="") as handle:
        writer = csv.DictWriter(handle, fieldnames=fieldnames)
        writer.writeheader()
        for result in report.case_results:
            writer.writerow({key: getattr(result, key) for key in fieldnames})

    return json_path, csv_path
