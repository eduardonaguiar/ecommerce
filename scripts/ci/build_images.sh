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
  context="services/${service}"
  if [[ ! -d "${context}" ]]; then
    echo "Skipping ${service}; context ${context} not found."
    continue
  fi
  image="${REGISTRY}/ecommerce-${service}:${TAG}"
  docker build -t "${image}" "${context}"
done
