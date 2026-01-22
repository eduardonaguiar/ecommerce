# Orders events

## Topics
- `orders.events`

## Event types
### order.created
Emitted after an order is created in Postgres with status `PENDING`.

**Envelope (JSON)**
```json
{
  "id": "string",
  "type": "order.created",
  "source": "orders",
  "time": "2024-01-01T00:00:00Z",
  "trace_id": "string",
  "span_id": "string",
  "request_id": "string",
  "version": "1",
  "data": {
    "orderId": "00000000-0000-0000-0000-000000000000",
    "amount": 0,
    "currency": "USD",
    "customerId": "string",
    "status": "PENDING",
    "createdAt": "2024-01-01T00:00:00Z"
  }
}
```

### order.confirmed
Emitted when the order transitions to `CONFIRMED` after both stock and payment events arrive.

**Envelope (JSON)**
```json
{
  "id": "string",
  "type": "order.confirmed",
  "source": "orders",
  "time": "2024-01-01T00:00:00Z",
  "trace_id": "string",
  "span_id": "string",
  "request_id": "string",
  "version": "1",
  "data": {
    "orderId": "00000000-0000-0000-0000-000000000000",
    "status": "CONFIRMED",
    "confirmedAt": "2024-01-01T00:00:00Z"
  }
}
```

### order.cancelled
Emitted when the order transitions to `CANCELLED` due to a stock failure or downstream rejection.

**Envelope (JSON)**
```json
{
  "id": "string",
  "type": "order.cancelled",
  "source": "orders",
  "time": "2024-01-01T00:00:00Z",
  "trace_id": "string",
  "span_id": "string",
  "request_id": "string",
  "version": "1",
  "data": {
    "orderId": "00000000-0000-0000-0000-000000000000",
    "status": "CANCELLED",
    "reason": "stock.failed",
    "cancelledAt": "2024-01-01T00:00:00Z"
  }
}
```

### stock.reserved
Consumed by Orders when inventory completes a reservation.

**Envelope (JSON)**
```json
{
  "id": "string",
  "type": "stock.reserved",
  "source": "inventory",
  "time": "2024-01-01T00:00:00Z",
  "trace_id": "string",
  "span_id": "string",
  "request_id": "string",
  "version": "1",
  "data": {
    "orderId": "00000000-0000-0000-0000-000000000000",
    "reservationId": "string"
  }
}
```

### stock.failed
Consumed by Orders when inventory cannot reserve stock.

**Envelope (JSON)**
```json
{
  "id": "string",
  "type": "stock.failed",
  "source": "inventory",
  "time": "2024-01-01T00:00:00Z",
  "trace_id": "string",
  "span_id": "string",
  "request_id": "string",
  "version": "1",
  "data": {
    "orderId": "00000000-0000-0000-0000-000000000000",
    "reason": "string"
  }
}
```

### payment.processed
Consumed by Orders when payment succeeds.

**Envelope (JSON)**
```json
{
  "id": "string",
  "type": "payment.processed",
  "source": "payments",
  "time": "2024-01-01T00:00:00Z",
  "trace_id": "string",
  "span_id": "string",
  "request_id": "string",
  "version": "1",
  "data": {
    "orderId": "00000000-0000-0000-0000-000000000000",
    "paymentId": "string"
  }
}
```

**Notes**
- Envelope fields follow the standard in `docs/events/envelope.md`.
- Orders publishes and consumes on the same topic in local compose (`orders.events`).
