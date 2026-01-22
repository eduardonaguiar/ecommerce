# Quickstart (Local)

## Prerequisites
- Make
- Docker (required for local infrastructure)

## Repository layout
- `services/` for service boundaries and APIs
- `infra/` for local infrastructure tooling
- `docs/` for architecture and operations docs
- `scripts/` for developer utilities
- `tests/` for system-level tests

## Get productive in 2 commands
```bash
cp .env.example .env
make up
```

## Infrastructure bootstrap (Docker Compose)
```bash
docker compose -f infra/compose/docker-compose.yml up -d
```

## Verification
```bash
docker compose -f infra/compose/docker-compose.yml ps
curl -fsSL http://localhost:9200/_cluster/health
curl -fsSL http://localhost:5601/api/status
```

Reference the full port list in [`docs/ops/port-map.md`](port-map.md).

## Useful commands
```bash
make urls
make logs
make smoke
```

## Local URLs (defaults)
- Web: http://localhost:3000
- Admin: http://localhost:3001
- API: http://localhost:8080
- Grafana: http://localhost:3000
- Prometheus: http://localhost:9090
- Loki: http://localhost:3100
- Tempo: http://localhost:3200
- PgAdmin: http://localhost:5050
- Kafka UI: http://localhost:8081
- MinIO Console: http://localhost:9001
- Mailpit: http://localhost:8025

## Notes
- Infrastructure services live under `infra/compose/docker-compose.yml`.
- This repo currently provides skeleton structure and DevEx commands only. Infra and services will be wired next in `infra/` and `services/`.
