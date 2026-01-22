# E2E Canon (Acceptance) — Study System

These scenarios define “done” for the lab. Each scenario should be executable locally and observable via Grafana/Kibana/Jaeger.

## E2E-001 — Saga success: Order CONFIRMED
Steps:
1) Seed catalog + stock
2) Create cart with items
3) POST /orders -> PENDING + OrderCreated
4) Inventory reserves -> StockReserved
5) Payments success -> PaymentProcessed(success)
Expected:
- Orders transitions to CONFIRMED
- OrderConfirmed event published
- Inventory commits stock
- Notification job enqueued and processed

## E2E-002 — Saga failure via payment: Order CANCELLED
Steps:
- Force payment failure
Expected:
- Orders transitions to CANCELLED
- Inventory releases reservation (if any)
- Notifications triggered for cancellation
- Payment not persisted as “effective”

## E2E-003 — Saga failure via stock: Order CANCELLED
Steps:
- Create order with insufficient stock
Expected:
- StockFailed leads to CANCELLED
- No stock commit occurs

## E2E-004 — Idempotency under duplicates
Steps:
- Replay PaymentProcessed(success) or StockReserved multiple times
Expected:
- Final order status unchanged
- No double-commit of stock
- Logs indicate dedupe/idempotent handling

## E2E-005 — CQRS convergence
Steps:
- Execute E2E-001
- Query read model for products and order status
Expected:
- Query eventually reflects confirmed status
- If lag induced, view converges later

## E2E-006 — Observability completeness
Expected signals:
- Trace in Jaeger spans Gateway -> Orders -> (Kafka) -> Inventory -> (Kafka) -> Orders -> Notifications -> Rabbit -> Worker
- Logs in Kibana filterable by correlationId
- Metrics in Grafana show: HTTP latency/errors, Kafka lag, Rabbit queue depth, cache hits/misses

## E2E-007 — Security baseline
Steps:
- Call protected endpoint without JWT
Expected: 401/403
- Call over HTTPS
Expected: success with valid token (dev token policy)
