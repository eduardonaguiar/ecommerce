# ADR-0005: Messaging Delivery Semantics

## Status
Accepted

## Context
Kafka and RabbitMQ are used for inter-service messaging. The event catalog already expects at-least-once delivery, and STOP requires explicit confirmation of delivery semantics for consumers and retry behavior.

## Options Considered
1. **At-most-once**
   - Pros: Simple consumer logic, no duplicates.
   - Cons: Message loss is possible, undermining eventual consistency and saga correctness.
2. **At-least-once**
   - Pros: No message loss; standard for Kafka/RabbitMQ; aligns with idempotent consumer requirement.
   - Cons: Requires deduplication/idempotency handling in consumers.
3. **Exactly-once**
   - Pros: Simplifies business logic by avoiding duplicates.
   - Cons: High complexity, broker-specific constraints, and often not practical in a lab setup.

## Decision
**Adopt at-least-once delivery semantics with idempotent consumers and deduplication.**

## Rationale
At-least-once aligns with the existing event catalog expectations and provides robust behavior without complex exactly-once tooling. It accelerates the lab by keeping implementation standard and observable (duplicate handling is a key learning objective).

## Consequences
- Consumers must be idempotent and able to handle duplicates safely.
- Retries and dead-lettering can be demonstrated without risking silent data loss.
- Observability will include lag/queue depth and duplicate-handling logs.
