#!/usr/bin/env bash
set -euo pipefail

CATALOG_BASE_URL=${CATALOG_BASE_URL:-http://localhost:8083}
CORRELATION_ID=${CORRELATION_ID:-seed-$(date +%s)}

payload=$(cat <<JSON
{
  "name": "Seeded Sneaker",
  "category": "Footwear",
  "price": 79.99,
  "currency": "USD",
  "description": "Seeded via scripts/testdata/seed.sh",
  "imageUrl": "https://picsum.photos/seed/seeded/400/400"
}
JSON
)

curl -fsSL -X POST "${CATALOG_BASE_URL}/admin/products" \
  -H "Content-Type: application/json" \
  -H "X-Correlation-Id: ${CORRELATION_ID}" \
  -d "${payload}" | sed -e 's/^/seeded: /'

echo "Seeded catalog product via ${CATALOG_BASE_URL} (correlationId=${CORRELATION_ID})."
