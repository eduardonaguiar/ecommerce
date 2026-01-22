# Functional Requirements (FR) — Canonical List

Rules:
- Each FR is atomic and testable.
- No new business scope beyond the descriptive model.
- Some “operational endpoints” are FRs because they are observable behaviors needed for the lab.

| FR-ID | Title | Requirement | Primary Owner | APIs | Events | Acceptance (minimal) |
|---|---|---|---|---|---|---|
| FR-ECOM-001 | API-first contracts | The system shall publish OpenAPI for each service. | All | GET /openapi or /swagger | — | Given service running, When fetching OpenAPI, Then it is accessible. |
| FR-ECOM-002 | Gateway enforces JWT | The Gateway shall validate JWT on protected routes. | Gateway | all proxied routes | — | Given no token, When calling protected route, Then 401/403. |
| FR-ECOM-003 | TLS termination | The Gateway shall expose HTTPS (dev TLS) for external access. | Gateway | HTTPS entrypoint | — | Given HTTPS URL, When calling, Then response is served. |
| FR-ECOM-004 | Health endpoints | Each service shall expose liveness/readiness endpoints. | All | /health/live, /health/ready | — | Given service running, endpoints return success. |
| FR-ECOM-005 | Metrics endpoint | Each service shall expose Prometheus metrics endpoint. | All | /metrics | — | Given Prometheus scrape, Then target is UP. |
| FR-ECOM-006 | Structured logging | Each service shall emit structured JSON logs with correlation identifiers. | All | — | — | Given request, Then logs contain traceId/correlationId. |
| FR-ECOM-010 | List products | The Catalog shall return a list of products. | Catalog | GET /products | — | Given seeded products, Then list is non-empty. |
| FR-ECOM-011 | Get product by id | The Catalog shall return a product by identifier. | Catalog | GET /products/{id} | — | Given existing id, Then product is returned; else 404. |
| FR-ECOM-012 | List categories | The Catalog shall return categories. | Catalog | GET /categories | — | Given seeded categories, Then list is returned. |
| FR-ECOM-013 | Publish catalog updates | The Catalog shall publish product update events for CQRS consumers. | Catalog | (seed/admin optional) | Kafka: ProductUpserted | Given a catalog change, Then ProductUpserted is published. |
| FR-ECOM-020 | Get cart | The Cart shall return the current cart state by cartId. | Cart | GET /carts/{cartId} | — | Given operations applied, Then cart reflects them. |
| FR-ECOM-021 | Add/update cart item | The Cart shall add or update a cart item quantity. | Cart | POST /carts/{cartId}/items | — | Given qty>0, Then item stored with qty. |
| FR-ECOM-022 | Remove cart item | The Cart shall remove an item from the cart. | Cart | DELETE /carts/{cartId}/items/{productId} | — | Given existing item, Then it is removed. |
| FR-ECOM-030 | Create order | The Orders service shall create an order in PENDING state. | Orders | POST /orders | Kafka: OrderCreated | Given valid order input, Then order is PENDING and OrderCreated exists. |
| FR-ECOM-031 | Get order | The Orders service shall return order by id. | Orders | GET /orders/{orderId} | — | Given order exists, Then status is returned. |
| FR-ECOM-032 | Finalize order by events | Orders shall finalize orders based on payment and stock events. | Orders | — | Kafka: consume Stock* + PaymentProcessed; produce OrderConfirmed/Cancelled | Given events arrive, Then order transitions deterministically. |
| FR-ECOM-040 | Reserve stock on order | Inventory shall reserve stock when OrderCreated is received. | Inventory | — | Kafka: consume OrderCreated; produce StockReserved/StockFailed | Given sufficient stock, Then StockReserved; else StockFailed. |
| FR-ECOM-041 | Commit/release stock | Inventory shall commit on OrderConfirmed and release on OrderCancelled. | Inventory | — | Kafka: consume OrderConfirmed/Cancelled | Given reservation exists, Then commit/release is applied. |
| FR-ECOM-050 | Process payment attempt | Payments shall process a payment attempt deterministically (mock). | Payments | POST /payments | Kafka: PaymentProcessed | Given forced success/failure, Then result matches. |
| FR-ECOM-051 | Persist successful payments | Payments shall persist only successful payments as effective records. | Payments | POST /payments | — | Given failure, Then no “effective” payment record exists. |
| FR-ECOM-060 | Enqueue notifications | Notifications API shall enqueue a Rabbit job upon order outcome events. | Notifications API | — | Kafka consume OrderConfirmed/Cancelled; Rabbit publish NotificationJob | Given order outcome, Then job is enqueued. |
| FR-ECOM-061 | Process notification jobs | Notifications Worker shall consume Rabbit jobs and execute simulated send. | Notifications Worker | — | Rabbit consume NotificationJob | Given job exists, Then worker logs “sent” and ack. |
| FR-ECOM-070 | Query products view | Query service shall expose product read-model queries. | Query | GET /query/products | Kafka consume ProductUpserted | Given events processed, Then query returns view. |
| FR-ECOM-071 | Query order view | Query service shall expose order status view by id. | Query | GET /query/orders/{id} | Kafka consume Order* + Payment* + Stock* | Given saga executed, Then view converges. |
| FR-ECOM-080 | Cache hot reads | Query (or Catalog) shall support Redis caching for hot read paths. | Query/Catalog | Query endpoints | — | Given repeated calls, Then cache hit metrics increase. |

Notes:
- FR-ECOM-013 assumes a minimal way to produce catalog changes (seed/admin). If not implemented, CQRS cannot be observed.
