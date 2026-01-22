# CQRS Convergence

This playbook confirms that the read model converges to the write model state.

## 1) Configure the query endpoint

If your query service is exposed directly, export the base URL and path before running the test:

```bash
export QUERY_BASE_URL=http://localhost:8088
export QUERY_ORDER_PATH=/orders
```

If you are routing through the gateway, also provide a JWT:

```bash
export QUERY_BASE_URL=https://localhost:8443/query
export QUERY_ORDER_PATH=/orders
export E2E_JWT=<valid-jwt>
```

## 2) Run the CQRS test

```bash
make e2e-cqrs
```

## 3) Validate convergence in logs

1. Open Kibana: <http://localhost:5601>.
2. Filter for the order id in both the **orders** and **query** service logs (if available).
3. Confirm the query read model eventually shows the same `CONFIRMED` status.

## Expected outcomes

- Orders write model reaches `CONFIRMED`.
- Query read model matches the same status within the poll timeout.
