# Cart API

## Overview
The Cart service provides an in-memory cart keyed by `cartId`. It supports fetching a cart, adding/updating an item, and removing an item. Quantity must be a positive integer; sending `quantity: 0` removes the item as a fast path.

## Base URL
- Direct: `http://localhost:8084`
- Gateway: `http://localhost:8080/cart`

## Endpoints

### GET /carts/{cartId}
Returns the cart. If the cart does not exist yet, an empty cart is returned.

**Response**
```json
{
  "cartId": "string",
  "items": [
    {
      "productId": "string",
      "quantity": 1
    }
  ]
}
```

### POST /carts/{cartId}/items
Adds or updates an item in the cart. If `quantity` is `0`, the item is removed.

**Request**
```json
{
  "productId": "string",
  "quantity": 1
}
```

**Response**
```json
{
  "cartId": "string",
  "items": [
    {
      "productId": "string",
      "quantity": 1
    }
  ]
}
```

### DELETE /carts/{cartId}/items/{productId}
Removes an item from the cart.

**Response**
```json
{
  "cartId": "string",
  "items": []
}
```

## Configuration
Environment variables used by the service:

| Variable | Default | Description |
| --- | --- | --- |
| `SERVICE_NAME` | `cart` | Service identifier for logging/telemetry. |
| `SERVICE_ENV` | `Development` | Service environment label. |
| `OTEL_EXPORTER_OTLP_ENDPOINT` | `http://otel-collector:4317` | OpenTelemetry collector endpoint. |

## Validation steps (Docker Compose)
1. Start the cart service (and OpenTelemetry collector if desired):
   ```bash
   docker compose -f infra/compose/docker-compose.yml up -d otel-collector cart
   ```
2. Add an item to a cart:
   ```bash
   curl -X POST http://localhost:8084/carts/cart-123/items \
     -H "Content-Type: application/json" \
     -d '{"productId":"sku-123","quantity":2}'
   ```
3. Remove the item with the fast-path (quantity = 0):
   ```bash
   curl -X POST http://localhost:8084/carts/cart-123/items \
     -H "Content-Type: application/json" \
     -d '{"productId":"sku-123","quantity":0}'
   ```
4. Fetch the cart:
   ```bash
   curl http://localhost:8084/carts/cart-123
   ```
