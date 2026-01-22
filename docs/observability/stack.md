# Observability Stack

This stack adds metrics, traces, and logs on top of the existing Compose services.

## Components

- **Prometheus**: scrapes metrics from the OpenTelemetry Collector and Jaeger.
- **Grafana**: pre-provisioned data sources (Prometheus, Jaeger, Elasticsearch) and a starter dashboard.
- **Jaeger**: trace storage and UI.
- **OpenTelemetry Collector**: OTLP ingest with trace export to Jaeger and metrics export to Prometheus.
- **Filebeat**: ships Docker container logs to Elasticsearch for Kibana.

## Configuration locations

- `infra/observability/prometheus/prometheus.yml`
- `infra/observability/otel-collector/config.yml`
- `infra/observability/grafana/provisioning/*`
- `infra/observability/grafana/dashboards/overview.json`
- `infra/observability/filebeat/filebeat.yml`

## Bring the stack up

```bash
docker compose -f infra/compose/docker-compose.yml up -d
```

## Validation steps

### Prometheus

1. Open `http://localhost:9090`.
2. Go to **Status → Targets** and verify `prometheus`, `otel-collector`, and `jaeger` are **UP**.

### Grafana

1. Open `http://localhost:3000` (default credentials: `admin` / `admin`).
2. Confirm the **Observability Overview** dashboard loads and shows the `Targets Up` gauge.
3. Check **Connections → Data sources** to verify Prometheus, Jaeger, and Elasticsearch are configured.

### Jaeger

1. Open `http://localhost:16686`.
2. Select the **Search** page and verify the UI is reachable. (Traces will appear once OTLP spans are sent to the collector at `http://localhost:4318` or `grpc://localhost:4317`.)

### OpenTelemetry Collector

1. Send test telemetry (example with OTLP/HTTP):
   ```bash
   curl -X POST http://localhost:4318/v1/traces -H 'Content-Type: application/json' -d '{"resourceSpans":[]}'
   ```
2. Confirm the collector logs show the request (visible in container logs).

### Logs in Kibana

1. Open `http://localhost:5601`.
2. Go to **Discover** and create/select the `filebeat-*` index pattern.
3. Verify Docker container logs are present.
