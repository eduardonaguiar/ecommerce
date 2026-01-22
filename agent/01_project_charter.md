# Project Charter — E-commerce System Design Lab (Project 1)

## Purpose
Build a backend-only e-commerce system with intentionally small business scope and rich architectural behavior to study:
- Microservices boundaries
- Event-Driven Architecture (Kafka + RabbitMQ)
- CQRS (read model projections)
- Saga (distributed workflow)
- Strong vs eventual consistency
- Idempotency and at-least-once delivery
- Caching and staleness
- Observability (Prometheus/Grafana, ELK, OpenTelemetry, Jaeger)
- Security & Privacy by Design

## Hard Constraints (Non-negotiable)
- Backend: C# / .NET (modern LTS)
- Architecture: API-first (OpenAPI for every service)
- Runtime: Docker Compose (single-machine local dev)
- System is self-contained (no shared infra with other projects)
- Observability: Prometheus + Grafana, ELK, OpenTelemetry + Jaeger
- Messaging: Kafka and RabbitMQ (explicitly different purposes)
- No frontend
- Domain scope must remain minimal

## Explicit Non-goals
Do NOT implement:
- User accounts / registration / profiles
- Promotions / coupons / loyalty
- Shipping, taxes, refunds/returns, chargebacks
- Recommendations, wishlists
- Marketplace/multi-seller
- Complex pricing rules

## Learning-first Principles
- Prefer explicit documentation over hidden design.
- Prefer behaviors that can be observed (logs, traces, metrics, lag, retries).
- Tradeoffs must be explicit (STOP list).
- E2E canon defines “done”.

## Services (high level)
- Gateway/API BFF
- Catalog
- Cart
- Orders (Saga coordinator, depending on STOP)
- Payments
- Inventory
- Notifications (API + Worker)
- Query (CQRS read model)

## Consistency Model (intent)
- Local strong consistency inside each service’s database.
- Cross-service consistency via events (eventual consistency).
- No distributed transactions.
