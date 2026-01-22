# Observability Contract (Logs, Metrics, Traces)

## Logging (structured JSON)
Required fields:
- timestamp
- level
- service.name
- environment
- traceId
- spanId (if available)
- correlationId (orderId for saga)
- message
- eventType (when logging event publish/consume)
PII rules:
- Do not log addresses, payment details, or any direct PII.
- If any sensitive field exists, it must be redacted/masked.

## Tracing (OpenTelemetry)
- Propagate context across HTTP (W3C Trace Context).
- For messaging, inject/extract trace context in event envelope headers or metadata.
- Each consume/publish must create spans with attributes:
  - messaging.system (kafka/rabbitmq)
  - messaging.destination (topic/queue)
  - messaging.operation (publish/consume)
  - eventType, correlationId

## Metrics (minimum useful set)
HTTP:
- request count by route/status
- request duration histogram
DB (if available via instrumentation):
- query duration
Messaging:
- Kafka consumer lag (by group/topic)
- Rabbit queue depth + ack/nack
Cache:
- redis hits/misses
- cache latency

## Dashboards (minimal)
- Service overview: RPS, latency, error rate
- Kafka: consumer lag and throughput
- Rabbit: queue depth, processing rate
- Cache: hit ratio, latency
- System: dependency health status

## Alerts (conceptual, optional for lab)
- Kafka lag above threshold
- Rabbit queue depth continuously increasing
- Error rate spikes
- Service unhealthy
