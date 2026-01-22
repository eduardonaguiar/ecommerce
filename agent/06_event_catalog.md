# Event Catalog — Kafka + RabbitMQ

## Event Envelope (minimal)
All events must include:
- eventId (unique)
- eventType
- eventVersion
- occurredAt (UTC)
- correlationId (orderId as primary correlation in saga)
- payload (minimal fields)

## Kafka Topics (suggested)
- `catalog.events`
- `orders.events`
- `payments.events`
- `inventory.events`

## Events

| Channel | Topic/Queue | Event Name | Producer | Consumers | Purpose | Delivery expectation |
|---|---|---|---|---|---|---|
| Kafka | catalog.events | ProductUpserted | Catalog | Query | Update product read model | at-least-once; idempotent projection |
| Kafka | orders.events | OrderCreated | Orders | Inventory, (Payments optional), Query | Start saga; reserve stock; project | at-least-once; consumers must dedupe |
| Kafka | inventory.events | StockReserved | Inventory | Orders, Query | Signal stock reserved | at-least-once |
| Kafka | inventory.events | StockFailed | Inventory | Orders, Query | Signal stock reservation failure | at-least-once |
| Kafka | payments.events | PaymentProcessed | Payments | Orders, Query | Signal payment outcome | at-least-once |
| Kafka | orders.events | OrderConfirmed | Orders | Inventory, Notifications API, Query | Finalize order success | at-least-once |
| Kafka | orders.events | OrderCancelled | Orders | Inventory, Notifications API, Query | Finalize order failure | at-least-once |
| RabbitMQ | notifications.jobs | NotificationJob | Notifications API | Notifications Worker | Execute side-effect send | at-least-once; worker must be idempotent |

## Minimal Payload Fields (to keep unambiguous)
- ProductUpserted: productId, name, categoryId, price (optional), updatedAt
- OrderCreated: orderId, cartId (optional), lines[{productId, qty}]
- StockReserved/Failed: orderId, reason (if failed)
- PaymentProcessed: orderId, status (success|failure), reason (optional)
- OrderConfirmed/Cancelled: orderId, finalStatus
- NotificationJob: orderId, type (confirmed|cancelled), channel (email simulated)

Notes:
- Schemas must be versioned (eventVersion).
- Consumers must handle duplicates.
