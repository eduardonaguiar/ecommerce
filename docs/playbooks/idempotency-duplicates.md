# Idempotency & Duplicate Event Handling

This playbook validates that duplicate payment events do not cause extra order state transitions.

## 1) Run the idempotency test

```bash
make e2e-idempotency
```

Capture the order id and correlation id printed by the test.

## 2) Validate logs in Kibana

1. Open Kibana: <http://localhost:5601>.
2. In **Discover**, set the time range to the last 15 minutes.
3. Filter for `service: orders` and the order id.
4. Confirm only one `orders.saga.transition` entry exists for `payment.processed`.

## 3) Validate payment attempts

1. Filter for `service: payments` and the same order id.
2. Confirm there are two payment attempts, but the order remains `CONFIRMED`.

## Expected outcomes

- The first payment transition confirms the order.
- The duplicate payment event does not change the order state (`CONFIRMED` remains).
