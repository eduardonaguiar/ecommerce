# Kafka Lag & Backpressure

This playbook introduces Kafka lag and validates consumer recovery.

## 1) Run the lag script

```bash
scripts/chaos/kafka_lag.sh
```

By default, this stops `ecommerce-notifications-api`, produces `order.confirmed` events, and restarts the consumer.

## 2) Observe consumer lag

### Kafka CLI

```bash
docker exec -i ecommerce-kafka kafka-consumer-groups \
  --bootstrap-server localhost:9092 \
  --group notifications-api \
  --describe
```

You should see a lag spike while the consumer is stopped and a return to zero after restart.

### Grafana overview

1. Open Grafana: <http://localhost:3000> (admin/admin).
2. Open the **Observability Overview** dashboard.
3. Watch for service availability changes and any request spikes as the consumer drains.

## Expected outcomes

- Lag increases while the consumer is stopped.
- Lag returns to zero after the consumer restarts.
- Downstream RabbitMQ queue fills briefly and then drains.
