# Notifications events

## Sources
- Kafka: `orders.events`
- RabbitMQ: `notifications.send`

## Consumed Kafka events
### order.confirmed
Consumed from `orders.events` and mapped to a notification job.

### order.cancelled
Consumed from `orders.events` and mapped to a notification job.

## RabbitMQ payload
Notifications jobs are serialized to JSON and published to the `notifications.send` queue.

```json
{
  "eventType": "order.confirmed",
  "orderId": "00000000-0000-0000-0000-000000000000",
  "status": "CONFIRMED",
  "reason": null,
  "occurredAt": "2024-01-01T00:00:00Z",
  "requestId": "string",
  "traceId": "string",
  "spanId": "string"
}
```

**Notes**
- `traceId`, `spanId`, and `requestId` are propagated for observability and correlation.
- Notification processing is best-effort; failures are logged and do not block order processing.
