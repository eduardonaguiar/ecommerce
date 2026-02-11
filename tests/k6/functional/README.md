# Functional E2E k6 Tests

## Purpose
These scripts validate **business semantics** of the E-commerce system from the perspective of an external consumer. They exercise full end-to-end flows through the **Gateway/BFF** and validate observable outcomes such as order state transitions, inventory effects, and CQRS convergence.

**Functional tests vs. technical tests:**
- **Functional E2E (this folder):** verify business behavior (order lifecycle, payment outcomes, cart semantics, CQRS convergence, notifications as a side effect).
- **Technical E2E (tests/k6/*.js):** verify infrastructure concerns (idempotency, retries, message lag, TLS, etc.).

## Scenarios Covered
- **Successful Purchase Flow:** browse catalog → cart → order → payment success → order confirmed → inventory reduced → query model updated.
- **Payment Failure Flow:** order → payment failure → order cancelled → inventory released → query model updated.
- **Out-of-Stock Flow:** drain inventory → create order → stock failure → order cancelled → payment not finalized.
- **Cart Semantics:** cart add/update/remove → invalid quantities rejected.
- **CQRS Convergence:** write model confirmed → query model eventually matches.
- **Notification Side Effects:** order confirmed and notifications API remains reachable as the public signal (not routed through the gateway).

## Running Locally (Docker Compose)
1. Start the full stack (gateway, catalog, cart, orders, payments, inventory, query, notifications):
   ```bash
   docker compose -f infra/compose/docker-compose.yml up -d
   ```
2. Provide a JWT for the gateway (either `E2E_JWT` or `JWT_HS256_SECRET`).
   - To generate a local HS256 token quickly:
     ```bash
     JWT_HS256_SECRET=dev-secret make jwt-dev
     export E2E_JWT=$(JWT_HS256_SECRET=dev-secret make -s jwt-dev)
     ```
3. Run the functional suite:
   ```bash
   make e2e-functional
   ```

### Optional environment variables
- `GATEWAY_BASE_URL` (default: `https://localhost:8443`)
- `QUERY_BASE_URL` (default: `https://localhost:8443/query`)
- `QUERY_ORDER_PATH` (default: `/orders`)
- `POLL_TIMEOUT_MS` (default: `60000`)
- `POLL_INTERVAL_MS` (default: `500`)
- `INVENTORY_DRAIN_COUNT` (default: `100`)
- `INVENTORY_PRODUCT_ID` (default: `default`)
- `NOTIFICATIONS_BASE_URL` (default: `http://localhost:8089`)

## Running in Kubernetes
1. Ensure the Gateway and services are reachable (port-forward or ingress).
2. Export the base URLs and JWT:
   ```bash
   export GATEWAY_BASE_URL=https://<gateway-host>
   export QUERY_BASE_URL=https://<gateway-host>/query
   export E2E_JWT=<valid-jwt>
   ```
3. Run the specific suites:
   ```bash
   make e2e-functional
   make e2e-functional-success
   make e2e-functional-failure
   make e2e-functional-cqrs
   ```

## Expected Behaviors
- Orders transition from `PENDING` → `CONFIRMED` or `CANCELLED` depending on payment and stock outcomes.
- Inventory availability decreases on confirmed purchases and is released on cancellations.
- The query/read model eventually reflects the same order state as the write model.
- Notifications are best-effort and do not block order correctness; tests only validate the API is reachable on `NOTIFICATIONS_BASE_URL`.

## Common Failure Interpretations
- **401/403 from Gateway:** missing/invalid JWT (`E2E_JWT` or `JWT_HS256_SECRET`).
- **Order stays `PENDING`:** event processing not running (Kafka, orders, payments, inventory) or poll timeout too low.
- **Query model never converges:** query service not running or misconfigured `QUERY_BASE_URL`/`QUERY_ORDER_PATH`.
- **Inventory assertions fail:** inventory service not running or default stock already drained; restart inventory or adjust `INVENTORY_DRAIN_COUNT`.
- **Notifications reachability fails:** notifications API not running or `NOTIFICATIONS_BASE_URL` misconfigured.
