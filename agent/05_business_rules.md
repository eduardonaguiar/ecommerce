# Business Rules (BR) — Canonical List + State Machines

## BR Canonical Table

| BR-ID | Rule Statement | Owner | Inputs / Preconditions | Outputs / Postconditions | Affected States |
|---|---|---|---|---|---|
| BR-ECOM-001 | Orders must be created with status PENDING. | Orders | POST /orders | Order persisted as PENDING; publish OrderCreated | Order: ∅ → PENDING |
| BR-ECOM-002 | Order becomes CONFIRMED only after PaymentProcessed(success) AND StockReserved are both observed. | Orders | correlated events | publish OrderConfirmed | PENDING → CONFIRMED |
| BR-ECOM-003 | Order becomes CANCELLED if StockFailed OR PaymentProcessed(failure) occurs. | Orders | correlated event | publish OrderCancelled | PENDING → CANCELLED |
| BR-ECOM-004 | Order finalization must be deterministic based only on correlated events (no hidden reads). | Orders | event history | same inputs ⇒ same status | lifecycle |
| BR-ECOM-005 | OrderCreated must be published only after local order persistence succeeds. | Orders | successful commit | event exists | — |
| BR-ECOM-006 | Inventory, on OrderCreated, must attempt all-or-nothing reservation and publish exactly one outcome: StockReserved or StockFailed. | Inventory | OrderCreated | publish StockReserved/Failed | Reservation: NONE → RESERVED / FAIL |
| BR-ECOM-007 | Inventory commits stock on OrderConfirmed. | Inventory | OrderConfirmed | decrement stock; close reservation | RESERVED → COMMITTED |
| BR-ECOM-008 | Inventory releases reservation on OrderCancelled. | Inventory | OrderCancelled | release reservation | RESERVED → RELEASED |
| BR-ECOM-009 | Payments persists only successful payments as “effective” records. | Payments | payment result | effective record only on success | Payment: ∅ → SUCCESS |
| BR-ECOM-010 | Payments must publish PaymentProcessed(success|failure) for each attempt, correlated to order. | Payments | attempt result | event exists | — |
| BR-ECOM-011 | Notifications must be triggered only after OrderConfirmed or OrderCancelled. | Notifications | outcome events | enqueue job | — |
| BR-ECOM-012 | Notification failure must not affect order correctness/state. | Notifications | worker failure | retries allowed | — |
| BR-ECOM-013 | Cart item quantity must be a positive integer. | Cart | add/update item | accept if qty>0 | — |
| BR-ECOM-014 | Cart reads must reflect latest cart state after operations. | Cart | GET cart | returns updated | — |
| BR-ECOM-015 | Catalog reads require only technical identifiers (no user identity). | Catalog | GET products | read-only | — |
| BR-ECOM-016 | Catalog changes must produce ProductUpserted event to feed CQRS views. | Catalog | catalog update | event exists | — |
| BR-ECOM-017 | CQRS read models are eventually consistent with event sources. | Query | consume events | views converge | — |
| BR-ECOM-018 | Distributed workflows must use Saga + compensations; no distributed transactions. | Orders/Inventory/Payments | event-driven flow | converge to confirmed/cancelled | lifecycle |

## State Machine — Orders (minimal)
States:
- PENDING
- CONFIRMED (final)
- CANCELLED (final)

Transitions:
- ∅ -> PENDING via POST /orders
- PENDING -> CONFIRMED via (StockReserved + PaymentProcessed(success))
- PENDING -> CANCELLED via (StockFailed OR PaymentProcessed(failure))

## State Machine — Inventory Reservation (derived)
States:
- NONE
- RESERVED
- COMMITTED
- RELEASED

Transitions:
- NONE -> RESERVED on successful reservation after OrderCreated
- NONE -> NONE on failure to reserve after OrderCreated (emit StockFailed)
- RESERVED -> COMMITTED on OrderConfirmed
- RESERVED -> RELEASED on OrderCancelled

## BR STOP Notes
- Discount/pricing rules are not required unless explicitly added (avoid scope creep).
- Cart qty=0 semantics is a decision point if you want strictness vs ergonomics.
- Stock reservation TTL is a decision point if you want timeout-based cancellation behavior.
