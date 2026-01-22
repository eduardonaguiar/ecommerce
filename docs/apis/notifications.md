# Notifications API

## Overview
The Notifications API is a best-effort side-effect service. It consumes `order.confirmed` and `order.cancelled` events from Kafka and enqueues notification jobs into RabbitMQ without impacting order correctness.

## Base URL
- Direct: `http://localhost:8089`

## Endpoints

### GET /
Returns service status.

### GET /health/ready
Returns readiness for Docker Compose health checks.

## Configuration
Environment variables used by the service:

| Variable | Default | Description |
| --- | --- | --- |
| `SERVICE_NAME` | `notifications-api` | Service identifier for logging/telemetry. |
| `SERVICE_ENV` | `Development` | Service environment label. |
| `OTEL_EXPORTER_OTLP_ENDPOINT` | `http://otel-collector:4317` | OpenTelemetry collector endpoint. |
| `KAFKA_BOOTSTRAP_SERVERS` | `kafka:9092` | Kafka bootstrap servers. |
| `KAFKA_TOPIC` | `orders.events` | Kafka topic containing order lifecycle events. |
| `KAFKA_GROUP_ID` | `notifications-api` | Kafka consumer group id. |
| `RABBITMQ_HOST` | `rabbitmq` | RabbitMQ host. |
| `RABBITMQ_PORT` | `5672` | RabbitMQ port. |
| `RABBITMQ_USERNAME` | `ecommerce` | RabbitMQ username. |
| `RABBITMQ_PASSWORD` | `ecommerce` | RabbitMQ password. |
| `RABBITMQ_QUEUE` | `notifications.send` | Queue for notification jobs. |

## Validation steps (Docker Compose)
1. Start the dependencies plus notifications services:
   ```bash
   docker compose -f infra/compose/docker-compose.yml up -d kafka rabbitmq notifications-api notifications-worker
   ```
2. Publish a sample order event to Kafka:
   ```bash
   docker exec -i ecommerce-kafka kafka-console-producer \
     --bootstrap-server localhost:9092 \
     --topic orders.events <<'EOF'
   {"id":"evt-1","type":"order.confirmed","source":"orders","time":"2024-01-01T00:00:00Z","trace_id":"","span_id":"","request_id":"manual","version":"1","data":{"orderId":"00000000-0000-0000-0000-000000000000","status":"CONFIRMED","confirmedAt":"2024-01-01T00:00:00Z"}}
   EOF
   ```
3. Inspect the notifications worker logs to confirm the simulated send:
   ```bash
   docker logs ecommerce-notifications-worker --tail=50
   ```
