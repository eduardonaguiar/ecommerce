# Chaos Scripts

These scripts introduce targeted faults for the lab services. Run them from the repo root with Docker Compose up.

## kafka_lag.sh
Stops a Kafka consumer, produces backlog events, then restarts the consumer to observe catch-up behavior.

```bash
scripts/chaos/kafka_lag.sh
```

**Defaults**
- Consumer: `ecommerce-notifications-api`
- Events: `order.confirmed`

**What to expect**
- Kafka lag spikes while the consumer is stopped.
- After restart, the consumer drains backlog and RabbitMQ receives notification jobs.

## stop_consumer.sh
Stops a specific consumer container for a fixed duration and restarts it.

```bash
CONTAINER=ecommerce-inventory scripts/chaos/stop_consumer.sh
```

**What to expect**
- Event-driven side effects pause.
- Consumer resumes and drains backlog on restart.

## db_down_inventory.sh
Stops Postgres to simulate an inventory database outage, then automatically restarts it.

```bash
scripts/chaos/db_down_inventory.sh
```

**What to expect**
- Inventory readiness may fail and logs show database connectivity errors.
- After restart, the inventory service recovers.

## rabbit_worker_down.sh
Stops the notifications worker, publishes a confirmed order event, and observes queue growth.

```bash
scripts/chaos/rabbit_worker_down.sh
```

**What to expect**
- RabbitMQ queue `notifications.send` accumulates messages while the worker is down.
- After restart, the worker drains the queue.
