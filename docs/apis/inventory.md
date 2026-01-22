# Inventory API

## Overview
The Inventory service owns stock reservations for orders. It consumes order lifecycle events from Kafka and publishes `stock.reserved` or `stock.failed` outcomes.

## Base URL
- Direct: `http://localhost:8087`
- Gateway: `http://localhost:8080/inventory` (debug-only endpoint)

## Endpoints

### GET /inventory/{productId}
Debug-only endpoint to inspect the default stock item.

**Response**
```json
{
  "productId": "default",
  "availableQuantity": 99,
  "reservedQuantity": 1,
  "updatedAt": "2024-01-01T00:00:00Z"
}
```

## Configuration
Environment variables used by the service:

| Variable | Default | Description |
| --- | --- | --- |
| `SERVICE_NAME` | `inventory` | Service identifier for logging/telemetry. |
| `SERVICE_ENV` | `Development` | Service environment label. |
| `OTEL_EXPORTER_OTLP_ENDPOINT` | `http://otel-collector:4317` | OpenTelemetry collector endpoint. |
| `POSTGRES_CONNECTION_STRING` | `Host=postgres;Port=5432;Username=ecommerce;Password=ecommerce;Database=inventory` | Postgres connection string. |
| `KAFKA_BOOTSTRAP_SERVERS` | `kafka:9092` | Kafka bootstrap servers. |
| `KAFKA_INVENTORY_TOPIC` | `inventory.events` | Kafka topic for inventory events. |
| `KAFKA_ORDERS_TOPIC` | `orders.events` | Kafka topic consumed for order events. |
| `KAFKA_GROUP_ID` | `inventory-saga` | Kafka consumer group id. |
| `INVENTORY_DEFAULT_PRODUCT_ID` | `default` | Default product id used for reservations. |
| `INVENTORY_DEFAULT_STOCK` | `100` | Initial available quantity seed. |
| `INVENTORY_DEFAULT_RESERVATION_QTY` | `1` | Quantity reserved per order. |

## Validation steps (Docker Compose)
1. Start Kafka, Postgres, and the inventory service:
   ```bash
   docker compose -f infra/compose/docker-compose.yml up -d kafka postgres inventory
   ```
2. Publish an `order.created` event to trigger a reservation:
   ```bash
   docker exec -i ecommerce-kafka kafka-console-producer \
     --bootstrap-server localhost:9092 \
     --topic orders.events <<'JSON'
   {"id":"evt-1","type":"order.created","source":"orders","time":"2024-01-01T00:00:00Z","trace_id":"","span_id":"","request_id":"manual","version":"1","data":{"orderId":"00000000-0000-0000-0000-000000000001","amount":25,"currency":"USD","customerId":"demo","status":"PENDING","createdAt":"2024-01-01T00:00:00Z"}}
   JSON
   ```
3. Watch the emitted event:
   ```bash
   docker exec -i ecommerce-kafka kafka-console-consumer \
     --bootstrap-server localhost:9092 \
     --topic inventory.events \
     --from-beginning \
     --max-messages 1
   ```
4. Inspect the default inventory record:
   ```bash
   curl http://localhost:8087/inventory/default
   ```
