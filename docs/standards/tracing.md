# Tracing standards

## Goals
- Consistent distributed tracing across services.
- Minimal fields to enable correlation in tests and logs.

## Context propagation
- Use W3C Trace Context headers.
  - `traceparent` is required for inbound/outbound HTTP.
  - `tracestate` is optional.
- For async jobs/events, include `trace_id` and `span_id` in the message envelope.

## Span naming
- Use `service.operation` (e.g., `orders.create`).
- Keep names stable; do not include ids in names.

## Span attributes (minimal)
| Attribute | Type | Description |
| --- | --- | --- |
| `service.name` | string | Service name. |
| `operation` | string | Operation name. |
| `request.id` | string | Per-request id. |
| `entity.id` | string | Primary entity id when applicable. |
| `error` | bool | `true` on failure. |

## Sampling
- Default to always-on in dev/test.
- Production sampling policy is environment-specific but must preserve errors.

## Linking
- If a new trace is created for an async continuation, add a link to the parent trace/span where supported.

## Test guidance
- Tests can assert that a `traceparent` header is present in outbound requests.
- Tests should not depend on trace ids being stable.
