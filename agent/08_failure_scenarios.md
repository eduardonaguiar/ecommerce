# Failure Scenarios (Study-First)

Each scenario must be reproducible locally (compose + scripts) and observable in logs/metrics/traces.

## FS-001 — Kafka consumer lag
- Trigger: stop Query consumer; generate events; restart consumer
- Expected: lag increases; later decreases; views converge
- Observe: Grafana (lag), Jaeger traces for publish, Kibana logs

## FS-002 — Duplicate delivery
- Trigger: replay same event payload or reprocess offsets
- Expected: no duplicated final effects (idempotency)
- Observe: Orders status unchanged; inventory not double-decremented

## FS-003 — Inventory DB down during reservation
- Trigger: stop inventory DB container; create order
- Expected: StockFailed or processing stalled (must be documented)
- Observe: Orders remains PENDING then CANCELLED (if you implement policy) or pending (if no TTL)

## FS-004 — Payments failure
- Trigger: force payment mock failure
- Expected: PaymentProcessed(failure) leads to OrderCancelled; inventory releases if reserved
- Observe: end-to-end trace + logs

## FS-005 — Notifications worker down
- Trigger: stop notifications worker; place orders
- Expected: orders unaffected; Rabbit queue grows; resumes later
- Observe: Rabbit queue depth, worker logs when restarted

## FS-006 — Cache staleness
- Trigger: update catalog; query cached endpoint
- Expected: cache may serve stale until TTL; eventual convergence after TTL/event-driven invalidation (if implemented)
- Observe: cache hit/miss metrics + view content changes
