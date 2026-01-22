#!/usr/bin/env bash
set -euo pipefail

ORDERS_BASE_URL=${ORDERS_BASE_URL:-http://localhost:8085}
PAYMENTS_BASE_URL=${PAYMENTS_BASE_URL:-http://localhost:8086}
INVENTORY_BASE_URL=${INVENTORY_BASE_URL:-http://localhost:8087}
CATALOG_BASE_URL=${CATALOG_BASE_URL:-http://localhost:8083}

curl -fsSL "${CATALOG_BASE_URL}/products" > /dev/null
curl -fsSL "${ORDERS_BASE_URL}/health/ready" > /dev/null
curl -fsSL "${PAYMENTS_BASE_URL}/health/ready" > /dev/null
curl -fsSL "${INVENTORY_BASE_URL}/health/ready" > /dev/null

echo "Smoke checks passed (catalog/orders/payments/inventory)."
