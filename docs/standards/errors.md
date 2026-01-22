# Error standards

## Goals
- Predictable error shapes for clients and tests.
- Minimal fields to support automation.

## HTTP error response shape
- Use JSON with a consistent envelope.

```json
{
  "error": {
    "code": "string",
    "message": "string",
    "request_id": "string",
    "details": {
      "field": "value"
    }
  }
}
```

## Field rules
- `code`: stable, machine-readable identifier (e.g., `orders.not_found`).
- `message`: human-readable summary, no secrets.
- `request_id`: required for correlation.
- `details`: optional map for validation or context; keep it small.

## Status mapping (default)
- `400` validation or malformed request.
- `401` unauthenticated.
- `403` unauthorized.
- `404` not found.
- `409` conflict.
- `429` rate limit.
- `500` unexpected server errors.

## Logging
- Log errors at `error` level with `error.code` and `request_id`.
- Do not log request bodies unless explicitly safe.

## Test guidance
- Tests should assert `error.code`, `error.message`, and `request_id` exist.
- Avoid strict matching on `details` unless testing validation.
