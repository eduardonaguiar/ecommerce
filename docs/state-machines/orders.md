# Orders state machine

## States
- `PENDING`
- `CONFIRMED`
- `CANCELLED`

## Inputs
- `stock.reserved`
- `stock.failed`
- `payment.processed`

## Transition rules
| Current state | Stock status | Payment status | Input | Next state | Notes |
| --- | --- | --- | --- | --- | --- |
| `PENDING` | `PENDING` | `PENDING` | `stock.reserved` | `PENDING` | Waiting on payment. |
| `PENDING` | `PENDING` | `PENDING` | `payment.processed` | `PENDING` | Waiting on stock. |
| `PENDING` | `PENDING` | `PENDING` | `stock.failed` | `CANCELLED` | Stock failure is terminal. |
| `PENDING` | `RESERVED` | `PENDING` | `payment.processed` | `CONFIRMED` | Both prerequisites satisfied. |
| `PENDING` | `FAILED` | `PENDING` | `payment.processed` | `CANCELLED` | Stock failure takes precedence. |
| `PENDING` | `PENDING` | `PROCESSED` | `stock.reserved` | `CONFIRMED` | Both prerequisites satisfied. |
| `CONFIRMED` | `RESERVED` | `PROCESSED` | any | `CONFIRMED` | Terminal state, no transitions. |
| `CANCELLED` | `FAILED` | any | any | `CANCELLED` | Terminal state, no transitions. |

## Determinism guarantees
- `stock.failed` always transitions to `CANCELLED` if the order is not already terminal.
- `CONFIRMED` requires both `stock.reserved` and `payment.processed` to be observed.
- Events are idempotent: repeated events in the same state do not change the outcome.
