# .NET service template

## What this template includes
- Minimal API with Swagger/OpenAPI enabled in all environments.
- Structured JSON logging aligned with `docs/standards/logging.md`.
- OpenTelemetry tracing + metrics export via OTLP.
- Health probes: `/health/live` and `/health/ready`.

## Layout
```
services/_template/
├── Dockerfile
├── .dockerignore
└── src/ServiceTemplate/
    ├── Program.cs
    ├── ServiceTemplate.csproj
    └── Logging/JsonLogFormatter.cs
```

## Configuration
| Environment variable | Default | Purpose |
| --- | --- | --- |
| `SERVICE_NAME` | `service-template` | Value for the `service` log field + OTel resource name. |
| `SERVICE_ENV` | `Development` | Value for the `env` log field. |
| `OTEL_EXPORTER_OTLP_ENDPOINT` | `http://otel-collector:4317` | OTLP endpoint for traces + metrics. |
| `ASPNETCORE_URLS` | `http://0.0.0.0:8080` | Bind address for the HTTP server. |

## Endpoints
| Endpoint | Description |
| --- | --- |
| `/` | Sample response payload. |
| `/swagger` | Swagger UI. |
| `/health/live` | Liveness probe (always healthy). |
| `/health/ready` | Readiness probe (self-check). |

## Logging behavior
The JSON log formatter emits the required fields (`timestamp`, `level`, `service`, `env`, `message`, `trace_id`, `span_id`, `request_id`) and adds optional `event`, `entity_id`, `duration_ms`, and `attrs` fields when present. The request middleware adds `request_id` from the ASP.NET Core `TraceIdentifier`.

## Running locally with Compose
```
docker compose -f infra/compose/docker-compose.yml up -d service-template
```

Then browse:
- `http://localhost:8088/`
- `http://localhost:8088/swagger`
- `http://localhost:8088/health/live`
- `http://localhost:8088/health/ready`
