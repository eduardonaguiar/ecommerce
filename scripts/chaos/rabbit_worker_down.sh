#!/usr/bin/env bash
set -euo pipefail

WORKER_CONTAINER=${WORKER_CONTAINER:-ecommerce-notifications-worker}
KAFKA_CONTAINER=${KAFKA_CONTAINER:-ecommerce-kafka}
RABBIT_CONTAINER=${RABBIT_CONTAINER:-ecommerce-rabbitmq}
QUEUE_NAME=${QUEUE_NAME:-notifications.send}

if ! docker ps --format '{{.Names}}' | grep -q "^${WORKER_CONTAINER}$"; then
  echo "Container ${WORKER_CONTAINER} is not running."
  exit 1
fi

echo "Stopping ${WORKER_CONTAINER} to simulate worker outage..."
docker stop "${WORKER_CONTAINER}" > /dev/null

ORDER_ID=$(cat /proc/sys/kernel/random/uuid)

payload=$(cat <<JSON
{"id":"evt-notify-${ORDER_ID}","type":"order.confirmed","source":"orders","time":"$(date -u +%Y-%m-%dT%H:%M:%SZ)","trace_id":"","span_id":"","request_id":"chaos","version":"1","data":{"orderId":"${ORDER_ID}","status":"CONFIRMED","confirmedAt":"$(date -u +%Y-%m-%dT%H:%M:%SZ)"}}
JSON
)

echo "Publishing order.confirmed event to trigger notification enqueue..."
echo "${payload}" | docker exec -i "${KAFKA_CONTAINER}" kafka-console-producer \
  --bootstrap-server localhost:9092 \
  --topic orders.events > /dev/null

sleep 2

echo "Queue depth (expect growth while worker is down):"
docker exec "${RABBIT_CONTAINER}" rabbitmqctl list_queues name messages | grep "${QUEUE_NAME}" || true

echo "Starting ${WORKER_CONTAINER}..."
docker start "${WORKER_CONTAINER}" > /dev/null

echo "Worker restarted. Monitor queue depth until it drains."
