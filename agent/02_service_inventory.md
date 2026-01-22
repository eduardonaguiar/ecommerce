# Service Inventory — Authoritative

This document defines service boundaries, owned data, external dependencies, and public APIs (high-level only).

## 1) Gateway / API BFF
- Responsibility: external entry point, routing, authn/authz enforcement, TLS termination
- Owned data: none
- Dependencies: JWT issuer config, downstream HTTP to internal services
- Public API surface: routes proxying downstream OpenAPI endpoints

## 2) Catalog Service
- Responsibility: product + category read APIs; produce product update events for CQRS
- Owned data: Products, Categories (minimal fields)
- Dependencies: primary store (STOP: SQL vs NoSQL), Kafka producer
- Public APIs:
  - GET /products
  - GET /products/{id}
  - GET /categories
  - (Optional minimal admin/seed mechanism, if documented)

## 3) Cart Service
- Responsibility: cart state keyed by cartId; add/remove/list items
- Owned data: Cart, CartItem
- Dependencies: its own store (can be simple), optional Redis (if used), none cross-service
- Public APIs:
  - GET /carts/{cartId}
  - POST /carts/{cartId}/items
  - DELETE /carts/{cartId}/items/{productId}

## 4) Orders Service
- Responsibility: create order; manage order lifecycle; react to payment/stock events; publish outcomes
- Owned data: Order, OrderLine, OrderStatus
- Dependencies: SQL DB, Kafka producer/consumer
- Public APIs:
  - POST /orders
  - GET /orders/{orderId}

## 5) Payments Service
- Responsibility: deterministic payment processing (mockable provider), publish PaymentProcessed
- Owned data: PaymentAttempt, PaymentRecord (success only as “effective”)
- Dependencies: SQL DB, Kafka producer
- Public APIs:
  - POST /payments
  - GET /payments/{paymentId}

## 6) Inventory Service
- Responsibility: reserve/commit/release stock; publish stock outcomes
- Owned data: StockItem, Reservation
- Dependencies: SQL DB, Kafka consumer/producer
- Public APIs:
  - (Optional) GET /inventory/{productId} for debug only (avoid making it authoritative)

## 7) Notifications API + Worker
- Responsibility:
  - API: consume order outcome events and enqueue jobs to RabbitMQ
  - Worker: consume Rabbit jobs and “send” (simulated), with retry
- Owned data: minimal (optional) DeliveryAttempt log (non-authoritative)
- Dependencies: Kafka consumer (API), Rabbit producer/consumer, optional store
- Public APIs:
  - (Optional debug endpoints only; prefer none)

## 8) Query Service (CQRS Read Model)
- Responsibility: consume events to build read-optimized views; serve query endpoints
- Owned data: read model indices/collections (non-authoritative, rebuildable)
- Dependencies: Query store (STOP), Kafka consumer, optional Redis cache
- Public APIs:
  - GET /query/products
  - GET /query/orders/{orderId}

## Shared Cross-cutting Requirements for all services
- OpenAPI published
- /health/live and /health/ready
- /metrics for Prometheus
- Structured JSON logs with correlation IDs
- OTel traces (HTTP + messaging propagation)
