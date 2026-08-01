# ADR 0001: Record Architecture Decisions

## Status
Accepted — 2026-08-01

## Context
The VPet AI transformation is a large, multi-phase effort that will be executed by multiple
autonomous coding agents and human engineers. Architectural decisions made early (e.g. Clean
Architecture, additive projects, plugin-based loading) affect every subsequent phase. Without
a record, decisions are lost or re-litigated, and agents may deviate from the intended design.

## Decision
We adopt the Architecture Decision Record (ADR) pattern (Michael Nygard format) in
`docs/adr/`. Every architectural decision — a choice that affects the structure, dependencies,
or boundaries of the system — must be recorded as an ADR before or alongside the implementing PR.

ADR format:
- **Status**: Proposed → Accepted or Rejected
- **Context**: why the decision is needed
- **Decision**: what was decided
- **Consequences**: trade-offs, risks, follow-ups

ADRs are numbered sequentially (`NNNN-title.md`) and committed to the repo.

## Consequences
- + Decisions are traceable and reviewable.
- + New agents can understand *why* the architecture is the way it is.
- − Minor overhead per architectural PR (one markdown file).
- − ADRs are immutable once Accepted; supersession requires a new ADR referencing the old.