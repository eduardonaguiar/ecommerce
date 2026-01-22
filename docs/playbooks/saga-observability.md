# Saga Observability (Jaeger + Kibana)

Use this playbook to trace a successful saga from Orders → Inventory → Payments and observe the emitted events.

## 1) Run the saga success test

```bash
make e2e-saga-success
```

Copy the `correlationId` printed by the k6 output.

## 2) Inspect traces in Jaeger

1. Open Jaeger: <http://localhost:16686>.
2. Select the **orders** service.
3. Set the time range to the last 15 minutes.
4. Search and open the trace corresponding to the test time window.
5. Follow the spans into **inventory** and **payments** to confirm the saga fan-out.

## 3) Inspect logs in Kibana

1. Open Kibana: <http://localhost:5601>.
2. In **Discover**, set the time range to the last 15 minutes.
3. Filter for `service: orders` and the order id printed by the k6 script.
4. Expand the `orders.saga.transition` log entries to confirm state transitions.

## Expected outcomes

- A single order transitions `PENDING → CONFIRMED` once stock and payment events arrive.
- Inventory emits `stock.reserved` and Logs show reservation commit on order confirmation.
