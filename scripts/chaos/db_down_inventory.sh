#!/usr/bin/env bash
set -euo pipefail

POSTGRES_CONTAINER=${POSTGRES_CONTAINER:-ecommerce-postgres}
INVENTORY_BASE_URL=${INVENTORY_BASE_URL:-http://localhost:8087}

cleanup() {
  echo "Starting ${POSTGRES_CONTAINER}..."
  docker start "${POSTGRES_CONTAINER}" > /dev/null || true
}
trap cleanup EXIT

if ! docker ps --format '{{.Names}}' | grep -q "^${POSTGRES_CONTAINER}$"; then
  echo "Container ${POSTGRES_CONTAINER} is not running."
  exit 1
fi

echo "Stopping ${POSTGRES_CONTAINER} to simulate inventory DB outage..."
docker stop "${POSTGRES_CONTAINER}" > /dev/null

set +e
curl -fsSL "${INVENTORY_BASE_URL}/health/ready" > /dev/null
health_status=$?
set -e

if [[ ${health_status} -eq 0 ]]; then
  echo "Inventory still reports ready while DB is down. Check logs for transient errors."
else
  echo "Inventory readiness probe failed as expected."
fi
