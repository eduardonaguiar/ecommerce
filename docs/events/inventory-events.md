# Inventory events

## Topics
- `inventory.events`

## Event types
### stock.reserved
Emitted after inventory reserves stock for an order.

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
Emitted when inventory cannot reserve stock for an order.

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
    "reason": "insufficient_stock"
  }
}
```

## Consumed events
Inventory consumes the following types from `orders.events`:
- `order.created`
- `order.confirmed`
- `order.cancelled`

**Notes**
- Envelope fields follow the standard in `docs/events/envelope.md`.
