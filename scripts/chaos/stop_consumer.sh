#!/usr/bin/env bash
set -euo pipefail

CONTAINER=${CONTAINER:-ecommerce-notifications-api}
DURATION_SECONDS=${DURATION_SECONDS:-30}

if ! docker ps --format '{{.Names}}' | grep -q "^${CONTAINER}$"; then
  echo "Container ${CONTAINER} is not running."
  exit 1
fi

echo "Stopping ${CONTAINER} for ${DURATION_SECONDS}s..."
docker stop "${CONTAINER}" > /dev/null
sleep "${DURATION_SECONDS}"

echo "Starting ${CONTAINER}..."
docker start "${CONTAINER}" > /dev/null

echo "${CONTAINER} restarted."
