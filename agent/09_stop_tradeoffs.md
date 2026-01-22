# STOP Tradeoffs — Open Decisions

These decisions must be documented via ADRs before implementation that depends on them.

## STOP-1 — Catalog Primary Store
Options:
- A) SQL relational
- B) NoSQL document
- C) Search engine as source-of-truth (rare)
Impact: query flexibility vs schema rigidity vs operational complexity.

## STOP-2 — Saga style (Orders flow)
Options:
- A) Choreography
- B) Orchestration (Orders as coordinator)
Impact: coupling, observability, failure handling complexity.

## STOP-3 — Cache TTL / invalidation strategy
Options:
- A) TTL-only
- B) TTL + invalidation by events
Impact: simplicity vs freshness guarantees.

## STOP-4 — CQRS Query Store choice
Options:
- A) Elasticsearch
- B) DocumentDB / SQL views
Impact: search capabilities vs simplicity.

## STOP-5 — Payment provider mocking strategy
Options:
- A) External stub provider service
- B) Internal deterministic mock
Impact: realism vs speed.

## STOP-6 — Event serialization (JSON vs Protobuf)
Options:
- A) JSON + version fields
- B) Protobuf + schema/tooling
Impact: DevEx vs strict schema governance.

## STOP-7 — Stock reservation expiration
Options:
- A) No expiration (simpler)
- B) Expiration after X minutes (needs X)
Impact: avoiding “stuck PENDING” vs extra complexity.

## STOP-8 — Trace sampling and telemetry retention
Options:
- A) 100% sampling + short retention
- B) sampling + longer retention
Impact: cost/volume vs debug depth.

## STOP-9 — Encryption at rest scope
Options:
- A) Storage-level encryption (dev)
- B) App-level encryption for sensitive fields
Impact: correctness/security vs complexity.

## STOP-10 — SLO numbers
Options:
- A) No numeric SLOs (qualitative only)
- B) Set simple SLOs for lab (requires picking numbers)
Impact: measurement discipline vs arbitrary targets.
