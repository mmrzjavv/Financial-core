from __future__ import annotations

from datetime import date

from e2e_simulator.api_client import ApiClient
from e2e_simulator.data_generators import random_amount, random_subject
from e2e_simulator.documents import upload_case_document
from e2e_simulator.models import CaseRunResult, SimulationPath
from e2e_simulator.workflows.base import WorkflowRunner

# Investment document types (DocumentType enum)
PITCH_DECK = 1
TAX_DOCS = 3
COMPANY_REG = 4
PRE_CONTRACT = 7
SIGNED_CONTRACT = 9
COMPANY_INTRO = 12
EMPLOYEE_INSURANCE = 13
TRIAL_BALANCE = 14
ACTIVITY_LICENSES = 15
CAPITAL_PLANS = 19

DE2_REQUIRED = (
    COMPANY_INTRO,
    EMPLOYEE_INSURANCE,
    TRIAL_BALANCE,
    TAX_DOCS,
    ACTIVITY_LICENSES,
    COMPANY_REG,
    CAPITAL_PLANS,
)


class InvestmentWorkflow(WorkflowRunner):
    module_name = "investment"
    base_path = ""

    def __init__(self, *args, **kwargs) -> None:
        super().__init__(*args, **kwargs)
        self.base_path = f"{self.config.api_prefix}/investmentcases"

    async def run_case(self, case_index: int, path: SimulationPath) -> CaseRunResult:
        self.steps = 0
        self.api_errors = []
        result = CaseRunResult(module=self.module_name, case_index=case_index, path=path)

        try:
            case_id = await self._create_case(case_index)
            result.case_id = case_id
            await self._fill_and_submit_de1(case_index, case_id)
            await self._maybe_enrich(case_id, case_index, path)

            if path == SimulationPath.REVISION:
                await self._revision_at_de1(case_index, case_id)
            elif path == SimulationPath.REJECTION:
                await self._reject_at_de1_review(case_index, case_id)
                await self._finalize_result(result, case_id)
                result.success = result.error is None
                result.steps_taken = self.steps
                result.api_errors = list(self.api_errors)
                return result

            await self._approve_de1(case_index, case_id)
            await self._fill_and_submit_de2(case_index, case_id)

            if path == SimulationPath.REVISION:
                await self._revision_at_de2(case_index, case_id)

            await self._approve_de2(case_index, case_id)
            await self._valuations(case_index, case_id)
            await self._legal_and_contracts(case_index, case_id)
            await self._financial_and_ceo(case_index, case_id, random_amount())
            await self._payment_and_complete(case_index, case_id)

            result.success = True
        except Exception as exc:
            result.error = str(exc)
            result.api_errors = list(self.api_errors)

        if result.case_id:
            await self._finalize_result(result, result.case_id)
        result.steps_taken = self.steps
        result.api_errors = list(self.api_errors)
        return result

    async def _finalize_result(self, result: CaseRunResult, case_id: str) -> None:
        token = self.auth.get_token("admin")
        snapshot = await self._get_case_snapshot(case_id, token)
        result.case_number = snapshot.get("caseNumber") or snapshot.get("CaseNumber")
        result.final_status = self._status_name(snapshot)
        result.final_phase = self._phase_name(snapshot)

    async def _inject_enrichment(self, case_id: str, case_index: int) -> None:
        token = self.auth.get_token("investment_expert")
        await self._call(
            case_index=case_index,
            step="post-comment",
            method="POST",
            path=f"{self.base_path}/{case_id}/comments",
            token=token,
            json_body={
                "phase": 1,
                "message": f"E2E enrichment comment for case #{case_index}",
                "isInternal": False,
            },
        )
        await upload_case_document(
            self.api,
            base_path=self.base_path,
            case_id=case_id,
            token=self.auth.get_token("applicant"),
            document_type=99,
            file_name=f"extra-{case_index}.pdf",
        )
        self.steps += 1

    async def _create_case(self, case_index: int) -> str:
        token = self.auth.get_token("applicant")

        async def _create():
            payload = await self._call(
                case_index=case_index,
                step="create-case",
                method="POST",
                path=self.base_path,
                token=token,
                json_body={"applicantType": 1},
            )
            case_id = ApiClient.pick_id(payload)
            if not case_id:
                raise ValueError("Create case response missing id")
            return case_id

        return await self._step(f"create case #{case_index}", _create)

    async def _fill_and_submit_de1(self, case_index: int, case_id: str) -> None:
        token = self.auth.get_token("applicant")
        amount = random_amount()

        async def _fill():
            await self._call(
                case_index=case_index,
                step="update-de1",
                method="PUT",
                path=f"{self.base_path}/{case_id}/data-entry1",
                token=token,
                json_body={"businessStage": 2, "requestedAmount": amount},
            )
            await upload_case_document(
                self.api,
                base_path=self.base_path,
                case_id=case_id,
                token=token,
                document_type=PITCH_DECK,
                file_name=f"pitch-{case_index}.pdf",
            )
            await self._call(
                case_index=case_index,
                step="submit-de1",
                method="POST",
                path=f"{self.base_path}/{case_id}/data-entry1/submit",
                token=token,
                json_body={"comment": "E2E auto submit DE1"},
            )

        await self._step("fill & submit DE1", _fill)

    async def _revision_at_de1(self, case_index: int, case_id: str) -> None:
        token = self.auth.get_token("investment_expert")

        async def _revise():
            await self._call(
                case_index=case_index,
                step="revise-de1",
                method="POST",
                path=f"{self.base_path}/{case_id}/data-entry1/revision-request",
                token=token,
                json_body={"message": "Please update requested amount wording."},
            )
            await self._call(
                case_index=case_index,
                step="resubmit-de1",
                method="POST",
                path=f"{self.base_path}/{case_id}/data-entry1/submit",
                token=self.auth.get_token("applicant"),
                json_body={"comment": "E2E resubmit after DE1 revision"},
            )

        await self._step("DE1 revision loop", _revise)

    async def _reject_at_de1_review(self, case_index: int, case_id: str) -> None:
        token = self.auth.get_token("investment_expert")

        async def _reject():
            await self._call(
                case_index=case_index,
                step="reject-case",
                method="POST",
                path=f"{self.base_path}/{case_id}/reject",
                token=token,
                json_body={"reason": "E2E rejection path at DE1 review"},
            )

        await self._step("reject at DE1 review", _reject)

    async def _approve_de1(self, case_index: int, case_id: str) -> None:
        token = self.auth.get_token("investment_expert")

        async def _approve():
            await self._call(
                case_index=case_index,
                step="approve-de1",
                method="POST",
                path=f"{self.base_path}/{case_id}/data-entry1/approve",
                token=token,
                json_body={"comment": "Approved by E2E simulator"},
            )

        await self._step("approve DE1", _approve)

    async def _fill_and_submit_de2(self, case_index: int, case_id: str) -> None:
        token = self.auth.get_token("applicant")

        async def _fill():
            await self._call(
                case_index=case_index,
                step="update-de2",
                method="PUT",
                path=f"{self.base_path}/{case_id}/data-entry2",
                token=token,
                json_body={
                    "investmentAttractionBasis": random_subject("جذب سرمایه", case_index),
                },
            )
            for doc_type in DE2_REQUIRED:
                await upload_case_document(
                    self.api,
                    base_path=self.base_path,
                    case_id=case_id,
                    token=token,
                    document_type=doc_type,
                    file_name=f"de2-{doc_type}-{case_index}.pdf",
                )
            await self._call(
                case_index=case_index,
                step="submit-de2",
                method="POST",
                path=f"{self.base_path}/{case_id}/data-entry2/submit",
                token=token,
                json_body={"comment": "E2E submit DE2"},
            )

        await self._step("fill & submit DE2", _fill)

    async def _revision_at_de2(self, case_index: int, case_id: str) -> None:
        token = self.auth.get_token("investment_expert")

        async def _revise():
            await self._call(
                case_index=case_index,
                step="revise-de2",
                method="POST",
                path=f"{self.base_path}/{case_id}/data-entry2/revision-request",
                token=token,
                json_body={"message": "Upload clearer financial documents."},
            )
            await self._call(
                case_index=case_index,
                step="resubmit-de2",
                method="POST",
                path=f"{self.base_path}/{case_id}/data-entry2/submit",
                token=self.auth.get_token("applicant"),
                json_body={"comment": "E2E resubmit DE2"},
            )

        await self._step("DE2 revision loop", _revise)

    async def _approve_de2(self, case_index: int, case_id: str) -> None:
        token = self.auth.get_token("investment_expert")

        async def _approve():
            await self._call(
                case_index=case_index,
                step="approve-de2",
                method="POST",
                path=f"{self.base_path}/{case_id}/data-entry2/approve",
                token=token,
                json_body={"comment": "DE2 approved"},
            )

        await self._step("approve DE2", _approve)

    async def _valuations(self, case_index: int, case_id: str) -> None:
        async def _run():
            expert = self.auth.get_token("investment_expert")
            manager = self.auth.get_token("investment_manager")
            await self._call(
                case_index=case_index,
                step="valuation-initial",
                method="POST",
                path=f"{self.base_path}/{case_id}/valuations",
                token=expert,
                json_body={"type": 1, "amount": random_amount(200_000_000, 3_000_000_000), "notes": "Initial"},
            )
            await self._call(
                case_index=case_index,
                step="approve-val-initial",
                method="POST",
                path=f"{self.base_path}/{case_id}/valuations/initial/approve",
                token=manager,
                json_body={"comment": "Initial valuation OK"},
            )
            await self._call(
                case_index=case_index,
                step="valuation-secondary",
                method="POST",
                path=f"{self.base_path}/{case_id}/valuations",
                token=expert,
                json_body={"type": 2, "amount": random_amount(200_000_000, 3_000_000_000), "notes": "Secondary"},
            )
            await self._call(
                case_index=case_index,
                step="approve-val-secondary",
                method="POST",
                path=f"{self.base_path}/{case_id}/valuations/secondary/approve",
                token=manager,
                json_body={"comment": "Secondary valuation OK"},
            )

        await self._step("valuations", _run)

    async def _legal_and_contracts(self, case_index: int, case_id: str) -> None:
        async def _run():
            legal = self.auth.get_token("legal_expert")
            applicant = self.auth.get_token("applicant")
            await upload_case_document(
                self.api,
                base_path=self.base_path,
                case_id=case_id,
                token=legal,
                document_type=PRE_CONTRACT,
                file_name=f"pre-contract-{case_index}.pdf",
            )
            await self._call(
                case_index=case_index,
                step="approve-pre-contract",
                method="POST",
                path=f"{self.base_path}/{case_id}/contracts/preliminary/approve",
                token=applicant,
                json_body={"comment": "Applicant approved pre-contract"},
            )
            await self._call(
                case_index=case_index,
                step="finalize-contract",
                method="POST",
                path=f"{self.base_path}/{case_id}/contracts/finalize-draft",
                token=legal,
                json_body={"comment": "Draft finalized"},
            )
            await self._call(
                case_index=case_index,
                step="confirm-signature",
                method="POST",
                path=f"{self.base_path}/{case_id}/contracts/confirm-signature",
                token=legal,
                json_body={"comment": "Signature confirmed"},
            )
            await upload_case_document(
                self.api,
                base_path=self.base_path,
                case_id=case_id,
                token=legal,
                document_type=SIGNED_CONTRACT,
                file_name=f"signed-{case_index}.pdf",
            )

        await self._step("legal contracts", _run)

    async def _financial_and_ceo(self, case_index: int, case_id: str, approved_amount: int) -> None:
        async def _run():
            expert = self.auth.get_token("investment_expert")
            financial = self.auth.get_token("financial_expert")
            ceo = self.auth.get_token("ceo")
            await self._call(
                case_index=case_index,
                step="save-worksheet",
                method="PUT",
                path=f"{self.base_path}/{case_id}/financial-worksheet",
                token=expert,
                json_body={
                    "bankName": "Demo Bank",
                    "iban": "IR000000000000000000000000",
                    "approvedAmount": approved_amount,
                    "paymentSchedule": "Single tranche",
                    "notes": "E2E worksheet",
                },
            )
            await self._call(
                case_index=case_index,
                step="submit-worksheet",
                method="POST",
                path=f"{self.base_path}/{case_id}/financial-worksheet/submit",
                token=expert,
                json_body={"comment": "Worksheet submitted"},
            )
            await self._call(
                case_index=case_index,
                step="approve-worksheet",
                method="POST",
                path=f"{self.base_path}/{case_id}/financial-worksheet/approve",
                token=financial,
                json_body={"comment": "Worksheet approved"},
            )
            await self._call(
                case_index=case_index,
                step="ceo-approve",
                method="POST",
                path=f"{self.base_path}/{case_id}/ceo-approval/approve",
                token=ceo,
                json_body={"comment": "CEO approved for payment"},
            )

        await self._step("financial worksheet & CEO", _run)

    async def _payment_and_complete(self, case_index: int, case_id: str) -> None:
        token = self.auth.get_token("financial_expert")
        amount = random_amount(50_000_000, 200_000_000)

        async def _pay():
            payment = await self._call(
                case_index=case_index,
                step="record-payment",
                method="POST",
                path=f"{self.base_path}/{case_id}/payments",
                token=token,
                json_body={
                    "amount": amount,
                    "paymentDate": date.today().isoformat(),
                    "transactionNumber": f"E2E-TX-{case_index}-{self.steps}",
                    "receiptS3Key": None,
                    "notes": "E2E payment",
                    "method": 1,
                    "status": 2,
                },
            )
            payment_id = ApiClient.pick_id(payment) if isinstance(payment, dict) else None
            if payment_id:
                await self._call(
                    case_index=case_index,
                    step="confirm-payment",
                    method="POST",
                    path=f"{self.base_path}/{case_id}/payments/{payment_id}/confirm",
                    token=token,
                    json_body=None,
                )

        await self._step("payment & completion", _pay)
