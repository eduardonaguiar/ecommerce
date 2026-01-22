# ADR-0002: Event Serialization Format

## Status
Accepted

## Context
The platform uses Kafka and RabbitMQ events across services. STOP-6 requires choosing between JSON and Protobuf for event serialization. The lab emphasizes speed-to-implementation and clarity of event payloads.

## Options Considered
1. **JSON with version fields (eventVersion)**
   - Pros: Human-readable, trivial tooling, easy to debug in logs, minimal setup.
   - Cons: No strict schema enforcement; larger payloads.
2. **Protobuf with schema/tooling**
   - Pros: Strong schema enforcement, smaller payloads, compatibility tooling.
   - Cons: Additional build/tooling complexity, harder to inspect in logs.

## Decision
**Use JSON with explicit `eventVersion` fields in the envelope.**

## Rationale
JSON accelerates the lab by avoiding schema tooling overhead and enabling quick inspection in logs and Kafka/RabbitMQ tooling. The explicit `eventVersion` field keeps evolution in scope without complex governance.

## Consequences
- Event payloads remain readable in logs and debugging tools.
- Consumers must be tolerant of extra/unknown fields as versions evolve.
- Future migration to schema-based serialization remains possible if needed.
