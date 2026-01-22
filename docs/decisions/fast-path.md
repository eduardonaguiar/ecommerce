# Fast-Path Decisions (Implementation Unblock)

This summary consolidates the STOP decisions required to start implementation for the E-commerce System Design Lab. These choices optimize for learning speed, simplicity, and observability.

## ✅ Resolved STOP Decisions

1. **Saga Style (STOP-2)**
   - **Decision:** Orchestration with Orders as the saga coordinator.
   - **Why:** Centralizes workflow logic for clarity and faster implementation while preserving event-driven integration.

2. **Event Serialization (STOP-6)**
   - **Decision:** JSON with explicit `eventVersion` fields in the envelope.
   - **Why:** Human-readable, minimal tooling, faster debugging.

3. **Catalog Primary Store (STOP-1)**
   - **Decision:** SQL relational database (e.g., Postgres).
   - **Why:** ACID guarantees, common tooling, and minimal operational overhead.

4. **CQRS Query Store (STOP-4)**
   - **Decision:** SQL read models (tables/materialized views).
   - **Why:** Avoids new infrastructure and keeps CQRS rebuild mechanics simple.

5. **Delivery Semantics (Event catalog + NFR-ECOM-013)**
   - **Decision:** At-least-once delivery with idempotent consumers.
   - **Why:** Standard for Kafka/RabbitMQ and aligns with event catalog expectations.

6. **Cache Strategy (STOP-3)**
   - **Decision:** TTL-only caching with a short TTL (e.g., 60s).
   - **Why:** Minimal complexity; still demonstrates cache behavior and staleness.

7. **Telemetry Sampling & Retention (STOP-8)**
   - **Decision:** 100% trace sampling with short retention (e.g., 3 days).
   - **Why:** Ensures every run is observable without setup overhead.

## Notes
- These decisions favor speed and clarity for a learning lab and can be revisited later.
- They align with existing functional/non-functional requirements and event catalog guidance.
