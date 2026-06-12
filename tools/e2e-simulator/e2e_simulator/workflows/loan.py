from __future__ import annotations

from datetime import date

from e2e_simulator.api_client import ApiClient
from e2e_simulator.data_generators import installment_schedule, random_amount, random_subject
from e2e_simulator.documents import upload_case_document
from e2e_simulator.models import CaseRunResult, SimulationPath
from e2e_simulator.workflows.base import WorkflowRunner

LOAN_DATA_ENTRY_DOCS = (1, 2, 3, 4, 5, 6, 7, 8, 9)
RAW_CONTRACT = 13
SIGNED_CONTRACT = 15
FINAL_CONTRACT = 22
PAYMENT_RECEIPT = 23


class LoanWorkflow(WorkflowRunner):
    module_name = "loan"
    base_path = ""

    def __init__(self, *args, **kwargs) -> None:
        super().__init__(*args, **kwargs)
        self.base_path = f"{self.config.api_prefix}/loancases"

    async def run_case(self, case_index: int, path: SimulationPath) -> CaseRunResult:
        self.steps = 0
        self.api_errors = []
        result = CaseRunResult(module=self.module_name, case_index=case_index, path=path)
        amount = random_amount(30_000_000, 400_000_000)

        try:
            case_id = await self._create_and_prepare(case_index, amount)
            result.case_id = case_id
            await self._submit_application(case_index, case_id)
            await self._maybe_enrich(case_id, case_index, path)

            if path == SimulationPath.REVISION:
                await self._credit_revision_loop(case_index, case_id, amount)

            if path == SimulationPath.REJECTION:
                await self._reject_at_ceo_initial(case_index, case_id, amount)
                await self._finalize_result(result, case_id)
                result.success = result.error is None
                result.steps_taken = self.steps
                result.api_errors = list(self.api_errors)
                return result

            await self._credit_and_ceo_initial(case_index, case_id, amount)
            await self._legal_setup(case_index, case_id, amount)
            await self._reviews_and_final_ceo(case_index, case_id, path, amount)
            await self._disburse_and_repay(case_index, case_id, amount)

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
        await upload_case_document(
            self.api,
            base_path=self.base_path,
            case_id=case_id,
            token=self.auth.get_token("applicant"),
            document_type=99,
            file_name=f"loan-extra-{case_index}.pdf",
        )
        self.steps += 1

    async def _create_and_prepare(self, case_index: int, amount: int) -> str:
        applicant = self.auth.get_token("applicant")

        async def _run():
            created = await self._call(
                case_index=case_index,
                step="create-case",
                method="POST",
                path=self.base_path,
                token=applicant,
                json_body={"applicantType": 1},
            )
            case_id = ApiClient.pick_id(created)
            if not case_id:
                raise ValueError("Missing loan case id")

            await self._call(
                case_index=case_index,
                step="begin-data-entry",
                method="POST",
                path=f"{self.base_path}/{case_id}/application/begin",
                token=applicant,
                json_body=None,
            )
            await self._call(
                case_index=case_index,
                step="update-application",
                method="PUT",
                path=f"{self.base_path}/{case_id}/application",
                token=applicant,
                json_body={
                    "requestedAmount": amount,
                    "requestedAmountInWords": f"{amount} ریال",
                    "facilitySubject": random_subject("تسهیلات", case_index),
                    "offeredGuarantees": "چک و سفته",
                    "applicantCategory": 1,
                    "representativePosition": "مدیرعامل",
                },
            )
            for doc_type in LOAN_DATA_ENTRY_DOCS:
                await upload_case_document(
                    self.api,
                    base_path=self.base_path,
                    case_id=case_id,
                    token=applicant,
                    document_type=doc_type,
                    file_name=f"loan-{doc_type}-{case_index}.pdf",
                )
            return case_id

        return await self._step("create & prepare loan application", _run)

    async def _submit_application(self, case_index: int, case_id: str) -> None:
        token = self.auth.get_token("applicant")

        async def _submit():
            await self._call(
                case_index=case_index,
                step="submit-application",
                method="POST",
                path=f"{self.base_path}/{case_id}/application/submit",
                token=token,
                json_body={"comment": "E2E loan submit"},
            )

        await self._step("submit loan application", _submit)

    async def _credit_revision_loop(self, case_index: int, case_id: str, amount: int) -> None:
        credit = self.auth.get_token("credit_expert")
        applicant = self.auth.get_token("applicant")

        async def _revise():
            await self._call(
                case_index=case_index,
                step="credit-revision",
                method="POST",
                path=f"{self.base_path}/{case_id}/credit/revision-request",
                token=credit,
                json_body={"message": "Clarify offered guarantees."},
            )
            await self._call(
                case_index=case_index,
                step="update-after-revision",
                method="PUT",
                path=f"{self.base_path}/{case_id}/application",
                token=applicant,
                json_body={
                    "requestedAmount": amount,
                    "facilitySubject": random_subject("تسهیلات", case_index),
                    "offeredGuarantees": "چک، سفته و وثیقه ملکی",
                    "applicantCategory": 1,
                },
            )
            await self._call(
                case_index=case_index,
                step="resubmit-application",
                method="POST",
                path=f"{self.base_path}/{case_id}/application/submit",
                token=applicant,
                json_body={"comment": "Resubmit after credit revision"},
            )

        await self._step("credit revision loop", _revise)

    async def _update_approval_detail(self, case_index: int, case_id: str, amount: int) -> None:
        credit = self.auth.get_token("credit_expert")
        await self._call(
            case_index=case_index,
            step="update-approval-detail",
            method="PUT",
            path=f"{self.base_path}/{case_id}/approval-detail",
            token=credit,
            json_body={
                "facilityType": 1,
                "contractSubject": random_subject("قرارداد", case_index),
                "approvedAmount": amount,
                "approvedAmountInWords": f"{amount} ریال",
                "repaymentMonths": 6,
                "gracePeriodMonths": 1,
                "annualProfitRatePercent": 18,
                "dailyPenaltyRatePercent": 0.05,
                "collateralDescription": "وثایق تسهیلات",
                "guarantorsDescription": "ضامنین",
                "expectedTotalProfit": round(amount * 0.09, 2),
            },
        )

    async def _credit_and_ceo_initial(self, case_index: int, case_id: str, amount: int) -> None:
        credit = self.auth.get_token("credit_expert")

        async def _run():
            await self._update_approval_detail(case_index, case_id, amount)
            await self._call(
                case_index=case_index,
                step="credit-approve",
                method="POST",
                path=f"{self.base_path}/{case_id}/credit/approve",
                token=credit,
                json_body={"comment": "Credit approved", "internalComment": "OK"},
            )
            await self._call(
                case_index=case_index,
                step="ceo-initial-approve",
                method="POST",
                path=f"{self.base_path}/{case_id}/ceo/initial/approve",
                token=self.auth.get_token("ceo"),
                json_body={"comment": "CEO initial approval"},
            )

        await self._step("credit & CEO initial", _run)

    async def _reject_at_ceo_initial(self, case_index: int, case_id: str, amount: int) -> None:
        credit = self.auth.get_token("credit_expert")

        async def _run():
            await self._update_approval_detail(case_index, case_id, amount)
            await self._call(
                case_index=case_index,
                step="credit-approve",
                method="POST",
                path=f"{self.base_path}/{case_id}/credit/approve",
                token=credit,
                json_body={"comment": "Credit approved", "internalComment": "OK"},
            )
            await self._call(
                case_index=case_index,
                step="ceo-initial-reject",
                method="POST",
                path=f"{self.base_path}/{case_id}/ceo/initial/reject",
                token=self.auth.get_token("ceo"),
                json_body={"message": "E2E CEO rejection"},
            )

        await self._step("CEO initial rejection", _run)

    async def _legal_setup(self, case_index: int, case_id: str, amount: int) -> None:
        legal = self.auth.get_token("legal_expert")

        async def _run():
            await upload_case_document(
                self.api,
                base_path=self.base_path,
                case_id=case_id,
                token=legal,
                document_type=RAW_CONTRACT,
                file_name=f"raw-{case_index}.pdf",
            )
            await self._call(
                case_index=case_index,
                step="upsert-installments",
                method="PUT",
                path=f"{self.base_path}/{case_id}/installments",
                token=legal,
                json_body={"installments": installment_schedule(amount, months=6, grace_rows=1)},
            )
            await self._call(
                case_index=case_index,
                step="legal-setup-complete",
                method="POST",
                path=f"{self.base_path}/{case_id}/legal/setup-complete",
                token=legal,
                json_body=None,
            )

        await self._step("legal raw contract & installments", _run)

    async def _reviews_and_final_ceo(
        self,
        case_index: int,
        case_id: str,
        path: SimulationPath,
        amount: int,
    ) -> None:
        applicant = self.auth.get_token("applicant")
        legal = self.auth.get_token("legal_expert")
        financial = self.auth.get_token("financial_expert")

        async def _run():
            await upload_case_document(
                self.api,
                base_path=self.base_path,
                case_id=case_id,
                token=applicant,
                document_type=SIGNED_CONTRACT,
                file_name=f"signed-{case_index}.pdf",
            )
            await self._call(
                case_index=case_index,
                step="signed-package",
                method="POST",
                path=f"{self.base_path}/{case_id}/signed-package/submit",
                token=applicant,
                json_body=None,
            )

            if path == SimulationPath.REVISION:
                await self._call(
                    case_index=case_index,
                    step="legal-revision",
                    method="POST",
                    path=f"{self.base_path}/{case_id}/legal/revision-request",
                    token=legal,
                    json_body={"message": "Fix signed contract scan quality."},
                )
                await upload_case_document(
                    self.api,
                    base_path=self.base_path,
                    case_id=case_id,
                    token=applicant,
                    document_type=SIGNED_CONTRACT,
                    file_name=f"signed-resubmit-{case_index}.pdf",
                )
                await self._call(
                    case_index=case_index,
                    step="signed-package-resubmit",
                    method="POST",
                    path=f"{self.base_path}/{case_id}/signed-package/submit",
                    token=applicant,
                    json_body=None,
                )

            await self._call(
                case_index=case_index,
                step="legal-approve",
                method="POST",
                path=f"{self.base_path}/{case_id}/legal/approve",
                token=legal,
                json_body={"comment": "Legal approved", "internalComment": "OK"},
            )
            await self._call(
                case_index=case_index,
                step="financial-approve",
                method="POST",
                path=f"{self.base_path}/{case_id}/financial/approve",
                token=financial,
                json_body={"comment": "Financial approved", "internalComment": "OK"},
            )
            await upload_case_document(
                self.api,
                base_path=self.base_path,
                case_id=case_id,
                token=legal,
                document_type=FINAL_CONTRACT,
                file_name=f"final-{case_index}.pdf",
            )
            await self._call(
                case_index=case_index,
                step="final-uploaded",
                method="POST",
                path=f"{self.base_path}/{case_id}/legal/final-uploaded",
                token=legal,
                json_body=None,
            )
            await self._call(
                case_index=case_index,
                step="ceo-final-approve",
                method="POST",
                path=f"{self.base_path}/{case_id}/ceo/final/approve",
                token=self.auth.get_token("ceo"),
                json_body={"comment": "CEO final approval"},
            )

        await self._step("legal/financial reviews & CEO final", _run)

    async def _disburse_and_repay(self, case_index: int, case_id: str, amount: int) -> None:
        financial = self.auth.get_token("financial_expert")
        applicant = self.auth.get_token("applicant")

        async def _run():
            receipt_key = await upload_case_document(
                self.api,
                base_path=self.base_path,
                case_id=case_id,
                token=financial,
                document_type=PAYMENT_RECEIPT,
                file_name=f"disburse-{case_index}.pdf",
            )
            await self._call(
                case_index=case_index,
                step="register-payment",
                method="POST",
                path=f"{self.base_path}/{case_id}/payments",
                token=financial,
                json_body={
                    "amount": amount,
                    "paymentDate": date.today().isoformat(),
                    "transactionNumber": f"LOAN-DISB-{case_index}",
                    "receiptS3Key": receipt_key,
                    "notes": "E2E disbursement",
                    "stageNumber": 1,
                },
            )

            installments = await self._call(
                case_index=case_index,
                step="list-installments",
                method="GET",
                path=f"{self.base_path}/{case_id}/installments",
                token=applicant,
            )
            items = installments if isinstance(installments, list) else []
            for item in items:
                if not isinstance(item, dict):
                    continue
                if item.get("isGracePeriod") or item.get("IsGracePeriod"):
                    continue
                if item.get("isPaid") or item.get("IsPaid"):
                    continue
                installment_id = ApiClient.pick_id(item)
                if not installment_id:
                    continue
                total = item.get("totalAmount") or item.get("TotalAmount") or 0
                receipt = await upload_case_document(
                    self.api,
                    base_path=self.base_path,
                    case_id=case_id,
                    token=applicant,
                    document_type=PAYMENT_RECEIPT,
                    file_name=f"repay-{installment_id}.pdf",
                )
                await self._call(
                    case_index=case_index,
                    step="mark-installment-paid",
                    method="POST",
                    path=f"{self.base_path}/{case_id}/installments/{installment_id}/mark-paid",
                    token=applicant,
                    json_body={
                        "paidDate": date.today().isoformat(),
                        "amount": total,
                        "transactionNumber": f"REP-{installment_id[:8]}",
                        "receiptS3Key": receipt,
                        "notes": "E2E repayment",
                    },
                )

            await self._call(
                case_index=case_index,
                step="complete-repayment",
                method="POST",
                path=f"{self.base_path}/{case_id}/repayment/complete",
                token=applicant,
                json_body=None,
            )

        await self._step("disbursement & repayment", _run)
