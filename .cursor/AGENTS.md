# Cursor Workspace

Lightweight index. **Do not duplicate** skill/workflow content here.

## Load model

| Layer | Path | When loaded |
|-------|------|-------------|
| Rules | `rules/*.mdc` | Every request (always) |
| Memory | `memory/engineering-memory.md` | On demand — stack, paths, philosophy |
| Skills | `skills/*.md` | Slash command or matching task |
| Workflows | `workflows/*.md` | Multi-step delivery loops |

## Philosophy

Simplicity · maintainability · correctness · production readiness. No forced DDD/CQRS/MediatR/event sourcing/microservices.

## Mandatory gates (enforced via skills)

FluentValidation · Mapster · business-process logging · persistence verification · security · EF performance · code review

## Skills

`/validation` `/mapping` `/logging` `/persistence` `/security` `/repository` `/ef` `/cqrs` `/refactor`

## Workflows

`workflows/feature-development.md` · `bug-fixing.md` · `code-review.md` · `release-readiness.md`

## Agents (roles)

| Role | Focus |
|------|-------|
| **Architect** | Contracts, validation/mapping/persistence plan, logging steps, test plan — design only unless asked to implement |
| **Developer** | Full implementation; all gates pass |
| **Reviewer** | `review.mdc` + workflows/code-review; **Reject** on gate failure |

Details: `memory/engineering-memory.md`
