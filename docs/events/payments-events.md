# Payments events

## Topics
- `payments.events`

## Event types

### payment.processed
Emitted after a payment attempt is evaluated (success or failure).

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
    "paymentId": "00000000-0000-0000-0000-000000000000",
    "status": "success",
    "reason": null,
    "amount": 120.50,
    "currency": "USD",
    "processedAt": "2024-01-01T00:00:00Z"
  }
}
```

**Notes**
- `status` is `success` or `failure`.
- `orderId` is the primary correlation key for downstream saga steps.
- Envelope fields follow the standard in `docs/events/envelope.md`.
