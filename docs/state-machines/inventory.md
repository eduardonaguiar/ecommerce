# Inventory reservation state machine

This state machine describes how the Inventory service manages stock reservations for an order. Each reservation is scoped to a single order and defaults to the configured `default` product with a quantity of 1 for local development.

## States
- **NONE**: No reservation exists for the order.
- **RESERVED**: Stock has been reserved (available decremented, reserved incremented).
- **FAILED**: Reservation attempt failed (insufficient stock).
- **COMMITTED**: Stock committed after order confirmation (reserved decremented).
- **RELEASED**: Reservation released after order cancellation (reserved decremented, available incremented).

## Transitions
| Trigger | Current State | Next State | Notes |
| --- | --- | --- | --- |
| `order.created` | NONE | RESERVED | All-or-nothing reservation; emits `stock.reserved`. |
| `order.created` | NONE | FAILED | Insufficient stock; emits `stock.failed`. |
| `order.created` | RESERVED/FAILED/COMMITTED/RELEASED | (unchanged) | Idempotent reprocessing; re-emits the original outcome. |
| `order.confirmed` | RESERVED | COMMITTED | Finalizes reservation; no event emitted. |
| `order.cancelled` | RESERVED | RELEASED | Returns stock to available pool; no event emitted. |
| `order.confirmed`/`order.cancelled` | FAILED/COMMITTED/RELEASED | (unchanged) | No-op. |

## Data effects
- **Reserve**: `available_quantity -= qty`, `reserved_quantity += qty`.
- **Commit**: `reserved_quantity -= qty`.
- **Release**: `available_quantity += qty`, `reserved_quantity -= qty`.

## Idempotency
Inventory stores one reservation per order (`order_id` unique). If an `order.created` event is reprocessed, the service looks up the existing reservation and re-emits the previously produced outcome without double-reserving stock.
