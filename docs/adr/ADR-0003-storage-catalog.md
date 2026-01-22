# ADR-0003: Catalog Primary Store

## Status
Accepted

## Context
The Catalog service needs a primary store for products and categories. STOP-1 requires deciding between SQL, NoSQL, or a search engine as source-of-truth. The lab favors a minimal operational footprint and clear data integrity.

## Options Considered
1. **SQL relational store (e.g., Postgres)**
   - Pros: Strong consistency, schema clarity, ACID transactions, widely understood.
   - Cons: Less flexible for unstructured attributes.
2. **NoSQL document store**
   - Pros: Flexible schema, easy to store nested product attributes.
   - Cons: Weaker relational guarantees, adds another storage technology to operate.
3. **Search engine as source-of-truth**
   - Pros: Fast search and filtering.
   - Cons: Operationally heavy and atypical as the primary source-of-truth.

## Decision
**Use a SQL relational store as the Catalog primary database.**

## Rationale
A relational store keeps the lab focused on core microservice and event-driven concerns rather than schema management. It accelerates implementation by leveraging a single, well-understood database type already needed by other services.

## Consequences
- Catalog data uses normalized schemas with explicit relationships.
- Search-heavy needs are deferred to CQRS projections if necessary.
- Fewer infrastructure components are required for the lab.
