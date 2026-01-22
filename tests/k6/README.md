# k6 E2E Test Suite

This folder contains the deterministic, API-only E2E checks for the E-commerce System Design Lab.

## Prerequisites

1. Start the platform:
   ```bash
   docker compose -f infra/compose/docker-compose.yml up -d --build
   ```
2. Generate the gateway development TLS certificate if you have not already:
   ```bash
   dotnet dev-certs https -ep services/gateway/certs/gateway-dev.pfx -p devpassword
   ```
3. Seed catalog data (optional but recommended for smoke checks):
   ```bash
   scripts/testdata/seed.sh
   ```

## Running tests

Each test runs as a single-iteration k6 script.

```bash
make e2e
make e2e-saga-success
make e2e-saga-failure-payment
make e2e-saga-failure-stock
make e2e-idempotency
make e2e-cqrs
make e2e-security
```

## Environment variables

| Variable | Default | Purpose |
| --- | --- | --- |
| `ORDERS_BASE_URL` | `http://localhost:8085` | Orders service base URL. |
| `PAYMENTS_BASE_URL` | `http://localhost:8086` | Payments service base URL. |
| `INVENTORY_BASE_URL` | `http://localhost:8087` | Inventory service base URL. |
| `CATALOG_BASE_URL` | `http://localhost:8083` | Catalog service base URL. |
| `GATEWAY_BASE_URL` | `https://localhost:8443` | Gateway TLS endpoint. |
| `QUERY_BASE_URL` | `ORDERS_BASE_URL` | CQRS read model base URL (override when query service is available). |
| `QUERY_ORDER_PATH` | `/orders` | Path for the query order read endpoint. |
| `CORRELATION_ID` | random | Shared correlation id propagated in `X-Correlation-Id`. |
| `POLL_INTERVAL_MS` | `500` | Poll interval for eventual consistency. |
| `POLL_TIMEOUT_MS` | `60000` | Poll timeout for eventual consistency. |
| `INVENTORY_DRAIN_COUNT` | `100` | Orders used to drain stock for stock failure scenario. |
| `E2E_JWT` | unset | JWT used for authenticated gateway checks. |
| `JWT_HS256_SECRET` | unset | Optional deterministic dev JWT signing secret. |
| `JWT_ISSUER` | `https://auth.local` | Issuer for generated dev JWT. |
| `JWT_AUDIENCE` | `ecommerce-api` | Audience for generated dev JWT. |
| `JWT_SUBJECT` | `k6-tester` | Subject for generated dev JWT. |
| `JWT_TTL_SECONDS` | `3600` | TTL for generated dev JWT. |

> Note: The gateway requires a valid JWT signature and metadata. If you do not have a compatible token, `e2e_security_tls.js` will only validate HTTPS reachability and rejection of unauthenticated requests.

## Expected outputs

Each test prints the shared `correlationId` in the console output. Use it to filter traces/logs in Jaeger or Kibana during debugging.
