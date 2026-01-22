# Health standards

## Goals
- Lightweight checks for orchestration and tests.
- Deterministic responses.

## Endpoints
- `GET /healthz` for liveness.
- `GET /readyz` for readiness.

## Response shape
- JSON only.

```json
{
  "status": "ok",
  "service": "string",
  "version": "string",
  "checks": {
    "dependency": "ok"
  }
}
```

## Status rules
- Liveness should return `200` when the process is running.
- Readiness returns `200` when dependencies are reachable; otherwise `503`.

## Check naming
- Use lowercase keys for dependencies (e.g., `db`, `redis`, `queue`).
- `checks` may be omitted for liveness.

## Test guidance
- Tests should assert `status` and `service` fields.
- Avoid checking `version` unless deterministic.
