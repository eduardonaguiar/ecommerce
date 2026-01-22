# Runbook: Docker Compose

Docker Compose is the canonical local execution model.

## Start
```bash
cd infra/compose
docker compose up -d
```

## Verify
- Gateway: http://localhost:8080
- Prometheus: http://localhost:9090
- Grafana: http://localhost:3000 (admin/admin)
- Jaeger: http://localhost:16686

## E2E Tests
```bash
make -C tests k6
```

## Troubleshooting
- Inspect logs: `docker compose logs -f <service>`
- Restart service: `docker compose restart <service>`
- Reset volumes: `docker compose down -v`
