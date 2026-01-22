# Orders API

## Overview
The Orders service acts as the saga orchestrator. Orders start in `PENDING`, then transition to `CONFIRMED` or `CANCELLED` based on incoming stock and payment events.

## Base URL
- Direct: `http://localhost:8085`
- Gateway: `http://localhost:8080/orders`

## Endpoints

### POST /orders
Creates a new order in `PENDING` and emits `order.created` after the database commit.

**Request**
```json
{
  "amount": 120.50,
  "currency": "USD",
  "customerId": "cust-123"
}
```

**Response**
```json
{
  "id": "00000000-0000-0000-0000-000000000000",
  "status": "PENDING",
  "stockStatus": "PENDING",
  "paymentStatus": "PENDING",
  "amount": 120.50,
  "currency": "USD",
  "customerId": "cust-123",
  "createdAt": "2024-01-01T00:00:00Z",
  "updatedAt": "2024-01-01T00:00:00Z"
}
```

### GET /orders/{orderId}
Returns the current order state.

**Response**
```json
{
  "id": "00000000-0000-0000-0000-000000000000",
  "status": "CONFIRMED",
  "stockStatus": "RESERVED",
  "paymentStatus": "PROCESSED",
  "amount": 120.50,
  "currency": "USD",
  "customerId": "cust-123",
  "createdAt": "2024-01-01T00:00:00Z",
  "updatedAt": "2024-01-01T00:00:00Z"
}
```

## Configuration
Environment variables used by the service:

| Variable | Default | Description |
| --- | --- | --- |
| `SERVICE_NAME` | `orders` | Service identifier for logging/telemetry. |
| `SERVICE_ENV` | `Development` | Service environment label. |
| `OTEL_EXPORTER_OTLP_ENDPOINT` | `http://otel-collector:4317` | OpenTelemetry collector endpoint. |
| `POSTGRES_CONNECTION_STRING` | `Host=postgres;Port=5432;Username=ecommerce;Password=ecommerce;Database=orders` | Postgres connection string. |
| `KAFKA_BOOTSTRAP_SERVERS` | `kafka:9092` | Kafka bootstrap servers. |
| `KAFKA_TOPIC` | `orders.events` | Kafka topic for order saga events. |
| `KAFKA_GROUP_ID` | `orders-saga` | Consumer group id for the saga orchestrator. |

## Validation steps (Docker Compose)
1. Start Kafka, Postgres, and the orders service:
   ```bash
   docker compose -f infra/compose/docker-compose.yml up -d kafka postgres orders
   ```
2. Create an order:
   ```bash
   curl -X POST http://localhost:8085/orders \
     -H "Content-Type: application/json" \
     -d '{"amount":120.50,"currency":"USD","customerId":"cust-123"}'
   ```
3. Capture the returned `id`, then simulate a stock reservation and payment completion using Kafka tooling:
   ```bash
   docker exec -i ecommerce-kafka kafka-console-producer \
     --bootstrap-server localhost:9092 \
     --topic orders.events <<'EOF'
   {"id":"evt-1","type":"stock.reserved","source":"inventory","time":"2024-01-01T00:00:00Z","trace_id":"","span_id":"","request_id":"manual","version":"1","data":{"orderId":"<ORDER_ID>","reservationId":"res-1"}}
   {"id":"evt-2","type":"payment.processed","source":"payments","time":"2024-01-01T00:00:00Z","trace_id":"","span_id":"","request_id":"manual","version":"1","data":{"orderId":"<ORDER_ID>","paymentId":"pay-1"}}
   EOF
   ```
4. Fetch the order to confirm it transitioned to `CONFIRMED`:
   ```bash
   curl http://localhost:8085/orders/<ORDER_ID>
   ```
