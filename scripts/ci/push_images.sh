#!/usr/bin/env bash
set -euo pipefail

REGISTRY=${REGISTRY:-localhost:5000}
TAG=${IMAGE_TAG:-latest}

services=(
  gateway
  catalog
  cart
  orders
  payments
  inventory
  notifications-api
  notifications-worker
  query
)

for service in "${services[@]}"; do
  image="${REGISTRY}/ecommerce-${service}:${TAG}"
  docker push "${image}"
done
