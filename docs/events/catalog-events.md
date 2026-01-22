# Catalog events

## Topics
- `catalog.product-upserted`

## Event types
### catalog.product.upserted
Emitted when a product is created or updated via the catalog admin upsert endpoint.

**Envelope (JSON)**
```json
{
  "id": "string",
  "type": "catalog.product.upserted",
  "source": "catalog",
  "time": "2024-01-01T00:00:00Z",
  "trace_id": "string",
  "span_id": "string",
  "request_id": "string",
  "version": "1",
  "data": {
    "id": "string",
    "name": "string",
    "category": "string",
    "price": 0,
    "currency": "USD",
    "description": "string",
    "imageUrl": "string",
    "updatedAt": "2024-01-01T00:00:00Z"
  }
}
```

**Notes**
- Envelope fields follow the standard in `docs/events/envelope.md`.
- `request_id` is populated from the HTTP request that triggered the upsert.
