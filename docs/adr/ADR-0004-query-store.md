# ADR-0004: CQRS Query Store Choice

## Status
Accepted

## Context
The Query service consumes events to build read-optimized views. STOP-4 requires selecting a query store technology that balances search capability with simplicity for the lab.

## Options Considered
1. **Elasticsearch**
   - Pros: Strong full-text search and filtering; common CQRS read store.
   - Cons: Additional operational complexity, tuning overhead, another moving part for a learning lab.
2. **SQL read models (tables/materialized views)**
   - Pros: Reuses existing relational database expertise, simpler to operate, easier local dev.
   - Cons: Less powerful search; may require explicit indexing strategies.

## Decision
**Use SQL read models (tables/materialized views) as the CQRS query store.**

## Rationale
A SQL-based query store accelerates the lab by avoiding a new infrastructure dependency while still enabling rebuildable read models from events. It keeps focus on event processing and eventual consistency rather than search cluster operations.

## Consequences
- Query read models are stored in relational tables and can be rebuilt from Kafka events.
- Advanced search is out of scope for the lab.
- Operational overhead remains minimal for local development.
