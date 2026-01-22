# Payments API

## Overview
The Payments service provides a deterministic mock payment processor. Every attempt is evaluated and a `payment.processed` event is published with `status: success|failure`. Only successful attempts are persisted as effective payments.

## Base URL
- Direct: `http://localhost:8086`
- Gateway: `http://localhost:8080/payments`

## Endpoints

### POST /payments
Processes a payment attempt.

**Request**
```json
{
  "orderId": "00000000-0000-0000-0000-000000000000",
  "amount": 120.50,
  "currency": "USD",
  "forceOutcome": "success"
}
```

**Response**
```json
{
  "id": "00000000-0000-0000-0000-000000000000",
  "orderId": "00000000-0000-0000-0000-000000000000",
  "amount": 120.50,
  "currency": "USD",
  "status": "SUCCESS",
  "failureReason": null,
  "effective": true,
  "createdAt": "2024-01-01T00:00:00Z"
}
```

**Deterministic mock rules**
- If `forceOutcome` is supplied, it must be `success` or `failure` and overrides the default result.
- Otherwise, the service sums the bytes of `orderId`; an even sum yields success and an odd sum yields failure.

### GET /payments/{paymentId}
Returns the payment attempt (successful or failed). Failed attempts are still returned, but only successful ones are persisted as effective payments.

**Response**
```json
{
  "id": "00000000-0000-0000-0000-000000000000",
  "orderId": "00000000-0000-0000-0000-000000000000",
  "amount": 120.50,
  "currency": "USD",
  "status": "FAILURE",
  "failureReason": "mock_declined",
  "effective": false,
  "createdAt": "2024-01-01T00:00:00Z"
}
```

## Configuration
Environment variables used by the service:

| Variable | Default | Description |
| --- | --- | --- |
| `SERVICE_NAME` | `payments` | Service identifier for logging/telemetry. |
| `SERVICE_ENV` | `Development` | Service environment label. |
| `OTEL_EXPORTER_OTLP_ENDPOINT` | `http://otel-collector:4317` | OpenTelemetry collector endpoint. |
| `POSTGRES_CONNECTION_STRING` | `Host=postgres;Port=5432;Username=ecommerce;Password=ecommerce;Database=payments` | Postgres connection string. |
| `KAFKA_BOOTSTRAP_SERVERS` | `kafka:9092` | Kafka bootstrap servers. |
| `KAFKA_TOPIC` | `payments.events` | Kafka topic for payment events. |

## Validation steps (Docker Compose)
1. Start Kafka, Postgres, and the payments service:
   ```bash
   docker compose -f infra/compose/docker-compose.yml up -d kafka postgres payments
   ```
2. Submit a payment with forced success:
   ```bash
   curl -X POST http://localhost:8086/payments \
     -H "Content-Type: application/json" \
     -d '{"orderId":"00000000-0000-0000-0000-000000000001","amount":120.50,"currency":"USD","forceOutcome":"success"}'
   ```
3. Watch the emitted event:
   ```bash
   docker exec -i ecommerce-kafka kafka-console-consumer \
     --bootstrap-server localhost:9092 \
     --topic payments.events \
     --from-beginning \
     --max-messages 1
   ```
4. Fetch the payment attempt:
   ```bash
   curl http://localhost:8086/payments/<PAYMENT_ID>
   ```
