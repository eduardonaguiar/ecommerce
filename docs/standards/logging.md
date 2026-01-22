# Logging standards

## Goals
- Structured, JSON-only logs that are machine-parseable.
- Minimal, deterministic fields for tests and automation.
- Every log line is a single JSON object.

## Required fields (all log lines)
| Field | Type | Description |
| --- | --- | --- |
| `timestamp` | string | RFC3339/ISO-8601 UTC (e.g., `2024-01-01T00:00:00Z`). |
| `level` | string | One of: `debug`, `info`, `warn`, `error`. |
| `service` | string | Service name (stable). |
| `env` | string | Environment (e.g., `dev`, `staging`, `prod`). |
| `message` | string | Human-readable summary. |
| `trace_id` | string | W3C trace id, if present. |
| `span_id` | string | W3C span id, if present. |
| `request_id` | string | Per-request id for HTTP or job. |

## Optional fields
- `event`: short action verb (e.g., `order.created`).
- `entity_id`: primary id tied to the event.
- `duration_ms`: number for timings.
- `error`: object with `type`, `message`, `stack` (only on `error`).
- `attrs`: object for additional structured data.

## Formatting rules
- No multi-line log entries.
- No PII or secrets.
- Use `error` level only when action failed and needs attention.
- Use `warn` for recoverable failures or retries.

## Example
```json
{"timestamp":"2024-01-01T00:00:00Z","level":"info","service":"orders","env":"dev","message":"created order","event":"order.created","entity_id":"ord_123","trace_id":"4bf92f3577b34da6a3ce929d0e0e4736","span_id":"00f067aa0ba902b7","request_id":"req_abc","duration_ms":12}
```

## Test guidance
- Tests should assert presence of required fields only.
- Avoid asserting on timestamps or stack traces.
