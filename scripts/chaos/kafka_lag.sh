#!/usr/bin/env bash
set -euo pipefail

CONSUMER_CONTAINER=${CONSUMER_CONTAINER:-ecommerce-notifications-api}
KAFKA_CONTAINER=${KAFKA_CONTAINER:-ecommerce-kafka}
EVENT_COUNT=${EVENT_COUNT:-5}

if ! docker ps --format '{{.Names}}' | grep -q "^${CONSUMER_CONTAINER}$"; then
  echo "Container ${CONSUMER_CONTAINER} is not running."
  exit 1
fi

echo "Stopping ${CONSUMER_CONTAINER} to create lag..."
docker stop "${CONSUMER_CONTAINER}" > /dev/null

ORDER_ID=$(cat /proc/sys/kernel/random/uuid)

payloads=""
for i in $(seq 1 "${EVENT_COUNT}"); do
  payloads+="{\"id\":\"evt-${i}-${ORDER_ID}\",\"type\":\"order.confirmed\",\"source\":\"orders\",\"time\":\"$(date -u +%Y-%m-%dT%H:%M:%SZ)\",\"trace_id\":\"\",\"span_id\":\"\",\"request_id\":\"chaos\",\"version\":\"1\",\"data\":{\"orderId\":\"${ORDER_ID}\",\"status\":\"CONFIRMED\",\"confirmedAt\":\"$(date -u +%Y-%m-%dT%H:%M:%SZ)\"}}"
  payloads+=$'\n'
done

echo "Producing ${EVENT_COUNT} order.confirmed events while consumer is down..."
echo "${payloads}" | docker exec -i "${KAFKA_CONTAINER}" kafka-console-producer \
  --bootstrap-server localhost:9092 \
  --topic orders.events > /dev/null

echo "Restarting ${CONSUMER_CONTAINER}..."
docker start "${CONSUMER_CONTAINER}" > /dev/null

echo "Consumer restarted. Watch logs or downstream queues to confirm catch-up."
