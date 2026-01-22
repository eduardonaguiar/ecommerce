# API Map (High-level)

## Gateway (external)
- Proxies to internal services; enforces JWT + TLS.

## Catalog
- GET /products
- GET /products/{id}
- GET /categories
- (Optional) POST /admin/products/{id} (seed/admin only; if implemented and documented)

## Cart
- GET /carts/{cartId}
- POST /carts/{cartId}/items
- DELETE /carts/{cartId}/items/{productId}

## Orders
- POST /orders
- GET /orders/{orderId}

## Payments
- POST /payments
- GET /payments/{paymentId}

## Inventory
- (Optional debug-only) GET /inventory/{productId}

## Query (CQRS)
- GET /query/products
- GET /query/orders/{orderId}

## Common endpoints (all services)
- GET /metrics
- GET /health/live
- GET /health/ready
- GET /swagger or /openapi
