# Event envelope (JSON)

## Goals
- Minimal, stable envelope for all events.
- Compatible with orchestration saga fast-path.

## Envelope schema
```json
{
  "id": "string",
  "type": "string",
  "source": "string",
  "time": "2024-01-01T00:00:00Z",
  "trace_id": "string",
  "span_id": "string",
  "request_id": "string",
  "version": "1",
  "data": {}
}
```

## Field rules
- `id`: unique event id (UUID or ULID).
- `type`: stable event name (e.g., `order.created`).
- `source`: service name (producer).
- `time`: RFC3339/ISO-8601 UTC.
- `trace_id`/`span_id`: optional but preferred for correlation.
- `request_id`: required if originating from an HTTP request or job.
- `version`: schema version for `data`.
- `data`: event payload (domain-specific, JSON object).

## Saga orchestration
- Commands and events share the same envelope.
- Saga state machine keys on `type` and `id`.
- Use `request_id` to correlate the saga initiation.

## Test guidance
- Tests should assert required envelope fields only.
- Avoid strict validation on `time` and `id` formats unless specifically under test.
