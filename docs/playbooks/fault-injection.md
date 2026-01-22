# Fault Injection (DB + Worker)

This playbook walks through database outages and worker interruptions.

## Inventory DB outage

```bash
scripts/chaos/db_down_inventory.sh
```

**Observe**
- Inventory readiness should fail while Postgres is down.
- Logs in Kibana show database connectivity errors.

## Notifications worker outage

```bash
scripts/chaos/rabbit_worker_down.sh
```

**Observe**
- RabbitMQ queue `notifications.send` grows while the worker is stopped.
- After restart, the queue drains and worker logs show delivery attempts.

## Expected outcomes

- Services recover automatically once dependencies restart.
- No order data loss occurs; events are replayed from Kafka or RabbitMQ.
