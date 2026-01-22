# ADR-0001: Saga Style (Orders Flow)

## Status
Accepted

## Context
The Orders workflow coordinates stock reservation and payment processing across services. We must choose a saga style that fits a learning-focused lab with minimal operational overhead while still surfacing distributed-systems behavior (events, idempotency, retries). STOP-2 requires documenting this decision before implementing the workflow.

## Options Considered
1. **Choreography (event-driven without central coordinator)**
   - Pros: Looser coupling, pure event-driven design, fewer central components.
   - Cons: Harder to observe/debug end-to-end, business logic scattered across services, more implicit coupling via event contracts.
2. **Orchestration (Orders service as coordinator)**
   - Pros: Centralized workflow logic, clearer state machine, easier to reason about for learners, simpler observability and failure handling.
   - Cons: Orders becomes a stronger dependency; slightly more coupling.

## Decision
**Choose orchestration with the Orders service as the saga coordinator.**

## Rationale
For a learning lab, orchestration accelerates implementation by centralizing the order lifecycle and keeping flow logic explicit. It reduces ambiguity in failure handling and makes observability and determinism easier to demonstrate while still exercising event-driven integrations.

## Consequences
- Orders owns the saga state transitions and emits OrderConfirmed/OrderCancelled.
- Other services remain autonomous for local transactions but follow Orders-driven outcomes.
- Observability can focus on a single workflow owner, simplifying traces and logs.
