from __future__ import annotations

import random
import string
from datetime import date, timedelta


def _digits(n: int) -> str:
    return "".join(random.choice(string.digits) for _ in range(n))


def random_national_id() -> str:
    return _digits(10)


def random_amount(min_value: int = 50_000_000, max_value: int = 500_000_000) -> int:
    step = 1_000_000
    low = min_value // step
    high = max_value // step
    return random.randint(low, high) * step


def random_subject(prefix: str, index: int) -> str:
    topics = ("توسعه محصول", "افزایش ظرفیت", "تحقیق و توسعه", "بازاریابی", "تجهیزات")
    return f"{prefix} — {random.choice(topics)} #{index}"


def random_beneficiary(index: int) -> tuple[str, str]:
    names = ("شرکت فناوران", "هلدینگ ساختمانی", "پیمانکار راه", "تأمین‌کننده صنعتی")
    return f"{random.choice(names)} {index}", random_national_id()


def future_date(months_ahead: int = 1) -> str:
    value = date.today() + timedelta(days=30 * months_ahead)
    return value.isoformat()


def installment_schedule(
    approved_amount: int,
    months: int = 6,
    grace_rows: int = 1,
) -> list[dict]:
    rows: list[dict] = []
    principal_each = round(approved_amount / max(months - grace_rows, 1), 2)
    profit_rate = 0.18
    for row in range(1, months + 1):
        is_grace = row <= grace_rows
        profit = 0 if is_grace else round(principal_each * profit_rate / 12, 2)
        principal = 0 if is_grace else principal_each
        total = principal + profit
        rows.append(
            {
                "rowNumber": row,
                "installmentDate": (date.today() + timedelta(days=30 * row)).isoformat(),
                "principalAmount": principal,
                "profitAmount": profit,
                "totalAmount": total,
                "fundShareOfPrincipal": principal,
                "fundShareOfProfit": profit,
                "fundShareOfTotal": total,
                "isGracePeriod": is_grace,
            }
        )
    return rows
