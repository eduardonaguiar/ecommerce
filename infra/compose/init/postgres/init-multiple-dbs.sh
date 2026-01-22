#!/usr/bin/env bash
set -euo pipefail

databases=(
  orders
  payments
  inventory
)

for db in "${databases[@]}"; do
  echo "Creating database: ${db}"
  psql -v ON_ERROR_STOP=1 --username "$POSTGRES_USER" <<-SQL
    CREATE DATABASE ${db};
SQL
done
