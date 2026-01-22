# ADR-0006: Cache Strategy for Read Paths

## Status
Accepted

## Context
Catalog and Query read paths should use caching to reduce database load. STOP-3 asks whether to rely on TTL-only caching or add event-driven invalidation. The lab aims for quick setup and observable cache behavior.

## Options Considered
1. **TTL-only caching**
   - Pros: Simple implementation, no extra event handling, easy to reason about.
   - Cons: Potential staleness until TTL expires.
2. **TTL + event-driven invalidation**
   - Pros: Fresher data with faster convergence after updates.
   - Cons: Added complexity in event wiring and cache invalidation logic.

## Decision
**Use TTL-only caching with a short default TTL (e.g., 60 seconds).**

## Rationale
TTL-only caching accelerates the lab by minimizing extra moving parts while still enabling cache metrics, hit/miss behavior, and staleness scenarios. A short TTL keeps data reasonably fresh without requiring invalidation plumbing.

## Consequences
- Reads may be stale for up to the TTL duration.
- Cache staleness can be demonstrated in failure scenarios.
- Event-driven invalidation can be added later as an optional enhancement.
