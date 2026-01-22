# Agent Context Pack — Project 1 (E-commerce System Design Lab)

This folder contains the authoritative context for the AI agent.

## How to use
Before executing any implementation prompt, the agent MUST:
1) Read `01_project_charter.md` (goals, constraints, non-goals)
2) Read `02_service_inventory.md` (service boundaries + owned data)
3) Read `03_functional_requirements.md` (FR canonical list)
4) Read `04_non_functional_requirements.md` (NFR canonical list)
5) Read `05_business_rules.md` (BR canonical list + state machines)
6) Read `06_event_catalog.md` (topics/queues, producers/consumers, minimal fields)
7) Read `07_api_map.md` (high-level endpoints)
8) Read `08_failure_scenarios.md` (expected behaviors)
9) Read `09_stop_tradeoffs.md` (STOP points)
10) Read `10_e2e_canon.md` (E2E acceptance canon)
11) Read `11_observability_contract.md` (logs/metrics/traces contract)

## Rules
- Do NOT invent features outside FR/NFR/BR.
- When a STOP decision exists, do not proceed with dependent implementation until resolved and documented (ADR).
- Keep the domain intentionally simple. The learning goal is System Design behavior and tradeoffs.

## Repo conventions (suggested)
- `services/<service-name>/` for each service
- `infra/` for docker-compose + configs
- `docs/` for documentation
- `tests/e2e/` for end-to-end canon validation
- `scripts/` for smoke/chaos scripts
