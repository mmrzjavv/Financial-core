from __future__ import annotations

from datetime import date

from e2e_simulator.api_client import ApiClient
from e2e_simulator.data_generators import random_amount, random_beneficiary, random_subject
from e2e_simulator.documents import upload_case_document
from e2e_simulator.models import CaseRunResult, SimulationPath
from e2e_simulator.workflows.base import WorkflowRunner

# GuaranteeDocumentType values
ESTABLISHMENT_GAZETTE = 2
FINANCIAL_STATEMENTS = 3
ACTIVITY_LICENSES = 4
BANK_TURNOVER = 5
CREDIT_INFO_FORM = 7
FORMATION_FEE = 8
ISSUANCE_REQUEST = 14
CEO_ID_CARDS = 12
DRAFT_CONTRACT = 18
SIGNED_CONTRACT = 19
FINAL_CONTRACT = 26
GUARANTEE_INSTRUMENT = 27
ISSUANCE_RECEIPT = 28

DATA_ENTRY_DOCS = (
    ESTABLISHMENT_GAZETTE,
    FINANCIAL_STATEMENTS,
    ACTIVITY_LICENSES,
    BANK_TURNOVER,
    CREDIT_INFO_FORM,
    FORMATION_FEE,
    ISSUANCE_REQUEST,
    CEO_ID_CARDS,
)


class GuaranteeWorkflow(WorkflowRunner):
    module_name = "guarantee"
    base_path = ""

    def __init__(self, *args, **kwargs) -> None:
        super().__init__(*args, **kwargs)
        self.base_path = f"{self.config.api_prefix}/guaranteecases"
        self._current_path = SimulationPath.HAPPY

    async def run_case(self, case_index: int, path: SimulationPath) -> CaseRunResult:
        self.steps = 0
        self.api_errors = []
        result = CaseRunResult(module=self.module_name, case_index=case_index, path=path)
        amount = random_amount(20_000_000, 300_000_000)

        self._current_path = path
        try:
            case_id = await self._create_and_prepare(case_index, amount)
            result.case_id = case_id
            await self._submit_application(case_index, case_id)
            await self._maybe_enrich(case_id, case_index, path)

            if path == SimulationPath.REVISION:
                await self._credit_revision_loop(case_index, case_id, amount)

            if path == SimulationPath.REJECTION:
                await self._reject_at_ceo_initial(case_index, case_id)
                await self._finalize_result(result, case_id)
                result.success = result.error is None
                result.steps_taken = self.steps
                result.api_errors = list(self.api_errors)
                return result

            await self._credit_approval_path(case_index, case_id, amount)
            await self._post_ceo_legal_path(case_index, case_id, path)
            await self._complete_issuance(case_index, case_id)

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
            file_name=f"enrichment-{case_index}.pdf",
        )
        self.steps += 1

    async def _create_and_prepare(self, case_index: int, amount: int) -> str:
        applicant = self.auth.get_token("applicant")
        beneficiary, beneficiary_id = random_beneficiary(case_index)

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
                raise ValueError("Missing guarantee case id")

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
                    "guaranteeType": 4,
                    "contractSubject": random_subject("ضمانت", case_index),
                    "isKnowledgeBasedProduct": False,
                    "beneficiaryName": beneficiary,
                    "beneficiaryNationalId": beneficiary_id,
                    "beneficiaryCompanyType": 1,
                    "applicantCategory": 1,
                    "applicantCategoryOther": None,
                    "applicantLegalForm": 1,
                    "requestedGuaranteeAmount": amount,
                    "initialValidityDays": 365,
                    "collateralDescription": "وثایق تست E2E",
                    "facilitySubject": random_subject("موضوع تسهیلات", case_index),
                },
            )
            for doc_type in DATA_ENTRY_DOCS:
                await upload_case_document(
                    self.api,
                    base_path=self.base_path,
                    case_id=case_id,
                    token=applicant,
                    document_type=doc_type,
                    file_name=f"guarantee-{doc_type}-{case_index}.pdf",
                )
            return case_id

        return await self._step("create & prepare application", _run)

    async def _submit_application(self, case_index: int, case_id: str) -> None:
        token = self.auth.get_token("applicant")

        async def _submit():
            await self._call(
                case_index=case_index,
                step="submit-application",
                method="POST",
                path=f"{self.base_path}/{case_id}/application/submit",
                token=token,
                json_body={"comment": "E2E submit guarantee application"},
            )

        await self._step("submit application", _submit)

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
                json_body={"message": "Please clarify collateral description."},
            )
            await self._call(
                case_index=case_index,
                step="update-after-revision",
                method="PUT",
                path=f"{self.base_path}/{case_id}/application",
                token=applicant,
                json_body={
                    "guaranteeType": 4,
                    "contractSubject": random_subject("ضمانت", case_index),
                    "beneficiaryName": random_beneficiary(case_index)[0],
                    "beneficiaryNationalId": random_beneficiary(case_index)[1],
                    "applicantCategory": 1,
                    "requestedGuaranteeAmount": amount,
                    "collateralDescription": "وثایق اصلاح‌شده E2E",
                    "facilitySubject": random_subject("موضوع", case_index),
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

    async def _reject_at_ceo_initial(self, case_index: int, case_id: str) -> None:
        amount = random_amount(20_000_000, 300_000_000)
        await self._credit_through_form(case_index, case_id, amount)
        ceo = self.auth.get_token("ceo")

        async def _reject():
            await self._call(
                case_index=case_index,
                step="ceo-initial-reject",
                method="POST",
                path=f"{self.base_path}/{case_id}/ceo/initial/reject",
                token=ceo,
                json_body={"message": "E2E CEO initial rejection"},
            )

        await self._step("CEO initial rejection", _reject)

    async def _credit_through_form(self, case_index: int, case_id: str, amount: int) -> None:
        credit = self.auth.get_token("credit_expert")

        async def _run():
            await self._call(
                case_index=case_index,
                step="credit-approve",
                method="POST",
                path=f"{self.base_path}/{case_id}/credit/approve",
                token=credit,
                json_body={"comment": "Credit approved", "internalComment": "Internal OK"},
            )
            await self._call(
                case_index=case_index,
                step="update-approval-form",
                method="PUT",
                path=f"{self.base_path}/{case_id}/approval-form",
                token=credit,
                json_body={
                    "guaranteeType": 4,
                    "guaranteeAmount": amount,
                    "contractSubject": random_subject("فرم تصویب", case_index),
                    "beneficiary": random_beneficiary(case_index)[0],
                    "issuanceDate": date.today().isoformat(),
                    "expiryDate": date(date.today().year + 1, date.today().month, date.today().day).isoformat(),
                    "activeDurationDays": 365,
                    "depositRatePercent": 10,
                    "depositAmount": round(amount * 0.1, 2),
                    "annualCommissionRatePercent": 2,
                    "commissionAmount": round(amount * 0.02, 2),
                    "collateralDescription": "وثایق مصوب",
                    "guarantorsDescription": "ضامنین مصوب",
                },
            )
            await self._call(
                case_index=case_index,
                step="submit-approval-form",
                method="POST",
                path=f"{self.base_path}/{case_id}/approval-form/submit",
                token=credit,
                json_body=None,
            )

        await self._step("credit review & approval form", _run)

    async def _credit_approval_path(self, case_index: int, case_id: str, amount: int) -> None:
        await self._credit_through_form(case_index, case_id, amount)

        async def _ceo():
            await self._call(
                case_index=case_index,
                step="ceo-initial-approve",
                method="POST",
                path=f"{self.base_path}/{case_id}/ceo/initial/approve",
                token=self.auth.get_token("ceo"),
                json_body={"comment": "CEO initial approval"},
            )

        await self._step("CEO initial approval", _ceo)

    async def _post_ceo_legal_path(self, case_index: int, case_id: str, path: SimulationPath) -> None:
        legal = self.auth.get_token("legal_expert")
        applicant = self.auth.get_token("applicant")
        financial = self.auth.get_token("financial_expert")

        async def _run():
            await upload_case_document(
                self.api,
                base_path=self.base_path,
                case_id=case_id,
                token=legal,
                document_type=DRAFT_CONTRACT,
                file_name=f"draft-{case_index}.pdf",
            )
            await self._call(
                case_index=case_index,
                step="draft-uploaded",
                method="POST",
                path=f"{self.base_path}/{case_id}/legal/draft-uploaded",
                token=legal,
                json_body=None,
            )
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
                    step="attachment-revision",
                    method="POST",
                    path=f"{self.base_path}/{case_id}/attachments/revision-request",
                    token=financial,
                    json_body={"message": "Re-upload signed attachments."},
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
                step="attachments-approve",
                method="POST",
                path=f"{self.base_path}/{case_id}/attachments/approve",
                token=financial,
                json_body={"comment": "Attachments approved", "internalComment": "OK"},
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

        await self._step("legal, financial attachments, CEO final", _run)

    async def _complete_issuance(self, case_index: int, case_id: str) -> None:
        financial = self.auth.get_token("financial_expert")

        async def _run():
            await upload_case_document(
                self.api,
                base_path=self.base_path,
                case_id=case_id,
                token=financial,
                document_type=GUARANTEE_INSTRUMENT,
                file_name=f"instrument-{case_index}.pdf",
            )
            await upload_case_document(
                self.api,
                base_path=self.base_path,
                case_id=case_id,
                token=financial,
                document_type=ISSUANCE_RECEIPT,
                file_name=f"receipt-{case_index}.pdf",
            )
            await self._call(
                case_index=case_index,
                step="issuance-uploaded",
                method="POST",
                path=f"{self.base_path}/{case_id}/issuance/uploaded",
                token=financial,
                json_body=None,
            )

        await self._step("issuance documents", _run)
