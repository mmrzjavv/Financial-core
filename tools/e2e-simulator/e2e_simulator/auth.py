from __future__ import annotations

import asyncio
from dataclasses import dataclass
from typing import Callable

from e2e_simulator.api_client import ApiClient, ApiClientError
from e2e_simulator.config import Persona, SimulatorConfig

LogFn = Callable[[str], None]


@dataclass
class Session:
    persona_key: str
    phone: str
    user_id: str | None
    access_token: str
    role: int


class AuthManager:
    def __init__(self, api: ApiClient, config: SimulatorConfig) -> None:
        self.api = api
        self.config = config
        self._sessions: dict[str, Session] = {}

    def get_token(self, persona_key: str) -> str:
        session = self._sessions.get(persona_key)
        if not session:
            raise KeyError(f"No session for persona '{persona_key}'. Provision users first.")
        return session.access_token

    async def login_persona(self, persona: Persona, log: LogFn | None = None) -> Session:
        if persona.key in self._sessions:
            return self._sessions[persona.key]

        if log:
            log(f"OTP login: {persona.key} ({persona.phone}) — waiting for API/SMS…")

        prefix = self.config.api_prefix
        await self.api.request(
            "POST",
            f"{prefix}/identity/users/send-otp",
            json_body={"phoneNumber": persona.phone},
            use_auth=False,
        )
        payload = await self.api.request(
            "POST",
            f"{prefix}/identity/users/verify-otp",
            json_body={"phoneNumber": persona.phone, "otpCode": self.config.dev_otp},
            use_auth=False,
        )
        token_model = payload.get("tokenModel") or payload.get("TokenModel") or {}
        user = payload.get("user") or payload.get("User") or {}
        access = token_model.get("accessToken") or token_model.get("AccessToken")
        if not access:
            raise ApiClientError("verify-otp response missing access token")

        session = Session(
            persona_key=persona.key,
            phone=persona.phone,
            user_id=str(user.get("id") or user.get("Id") or "") or None,
            access_token=access,
            role=int(user.get("roleNumber") or user.get("RoleNumber") or persona.role),
        )
        self._sessions[persona.key] = session
        if log:
            log(f"OTP login OK: {persona.key}")
        return session

    async def register_persona(self, persona: Persona, log: LogFn | None = None) -> str | None:
        prefix = self.config.api_prefix
        try:
            payload = await self.api.request(
                "POST",
                f"{prefix}/identity/users",
                json_body={
                    "phoneNumber": persona.phone,
                    "email": f"{persona.key}@e2e.test",
                    "firstName": persona.first_name,
                    "lastName": persona.last_name,
                    "nationalCode": None,
                },
                use_auth=False,
            )
            user_id = ApiClient.pick_id(payload)
            if log:
                log(f"Registered user {persona.key} ({persona.phone})")
            return user_id
        except ApiClientError as exc:
            if self._is_conflict(exc):
                if log:
                    log(f"User already exists: {persona.key} ({persona.phone})")
                return None
            raise

    @staticmethod
    def _is_conflict(exc: ApiClientError) -> bool:
        if exc.status_code == 409:
            return True
        message = str(exc).lower()
        return "conflict" in message or "already registered" in message or "قبلاً" in message

    def _bootstrap_persona(self, personas: list[Persona]) -> Persona:
        seed_phone = (self.config.seed_admin_phone or "").strip()
        if seed_phone:
            existing = next((p for p in personas if p.phone == seed_phone), None)
            if existing:
                return existing
            return Persona(
                key="seed_admin",
                phone=seed_phone,
                role=100,
                first_name="Seed",
                last_name="Admin",
            )

        admin = next((p for p in personas if p.key == "admin"), None)
        if admin:
            return admin
        return personas[0]

    async def _load_user_ids_by_phone(self, token: str) -> dict[str, str]:
        prefix = self.config.api_prefix
        mapping: dict[str, str] = {}
        skip = 0
        take = 100

        while True:
            payload = await self.api.request(
                "GET",
                f"{prefix}/identity/users?take={take}&skip={skip}",
                token=token,
            )
            items: list = []
            if isinstance(payload, list):
                items = payload
            elif isinstance(payload, dict):
                items = (
                    payload.get("items")
                    or payload.get("Items")
                    or payload.get("list")
                    or payload.get("data")
                    or []
                )
                if not isinstance(items, list):
                    items = [payload] if payload.get("id") or payload.get("Id") else []

            if not items:
                break

            for user in items:
                if not isinstance(user, dict):
                    continue
                phone = user.get("phoneNumber") or user.get("PhoneNumber")
                user_id = user.get("id") or user.get("Id")
                if phone and user_id:
                    mapping[str(phone)] = str(user_id)

            if len(items) < take:
                break
            skip += take

        return mapping

    async def _login_personas_parallel(
        self,
        personas: list[Persona],
        log: LogFn | None = None,
        concurrency: int = 3,
    ) -> None:
        semaphore = asyncio.Semaphore(max(1, concurrency))

        async def login_one(persona: Persona) -> None:
            if persona.key in self._sessions:
                return
            async with semaphore:
                await self.login_persona(persona, log)

        await asyncio.gather(*[login_one(persona) for persona in personas])

    async def ensure_users_provisioned(self, log: LogFn | None = None) -> None:
        personas = self.config.personas
        if not personas:
            raise ApiClientError("No personas configured in config.yaml")

        if log:
            log("Step 1/4 — register E2E users (fast, no SMS)…")

        prefix = self.config.api_prefix
        bootstrap = self._bootstrap_persona(personas)
        all_personas = list(personas)
        if bootstrap.key not in {p.key for p in personas}:
            all_personas.append(bootstrap)

        user_ids: dict[str, str | None] = {}
        for persona in all_personas:
            user_ids[persona.key] = await self.register_persona(persona, log)

        if log:
            log(
                f"Step 2/4 — bootstrap admin OTP login ({bootstrap.phone}); "
                "first SMS call, may take 10–30s…"
            )

        bootstrap_session = await self.login_persona(bootstrap, log)
        user_ids[bootstrap.key] = bootstrap_session.user_id
        if log:
            log(f"Bootstrap ready — role {bootstrap_session.role}")

        if log:
            log("Step 3/4 — resolve user IDs and assign roles (no SMS)…")

        directory = await self._load_user_ids_by_phone(bootstrap_session.access_token)
        for persona in personas:
            user_id = user_ids.get(persona.key) or directory.get(persona.phone)
            if not user_id:
                if log:
                    log(f"Resolving id for {persona.key} via single OTP login…")
                session = await self.login_persona(persona, log)
                user_id = session.user_id
            if not user_id:
                raise ApiClientError(f"Could not resolve user id for persona '{persona.key}'")
            user_ids[persona.key] = user_id

            await self.api.request(
                "PUT",
                f"{prefix}/identity/users/{user_id}",
                token=bootstrap_session.access_token,
                json_body={"role": persona.role, "isActive": True},
            )
            if log:
                log(f"Assigned role {persona.role} -> {persona.key}")

        pending = [p for p in personas if p.key not in self._sessions]
        if pending:
            if log:
                log(
                    f"Step 4/4 — OTP login for {len(pending)} workflow personas "
                    f"(parallel, ~10–30s each batch)…"
                )
            await self._login_personas_parallel(pending, log, concurrency=3)

        if log:
            log(f"All {len(personas)} personas ready.")
