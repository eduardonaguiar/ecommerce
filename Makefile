SHELL := /bin/bash

-include .env

APP_ENV ?= local
LOG_LEVEL ?= info
API_BASE_URL ?= http://localhost:8080
WEB_BASE_URL ?= http://localhost:3000
ADMIN_UI_URL ?= http://localhost:3001
POSTGRES_URL ?= postgres://app:app@localhost:5432/ecommerce
REDIS_URL ?= redis://localhost:6379/0
OTEL_EXPORTER_OTLP_ENDPOINT ?= http://localhost:4317
GRAFANA_URL ?= http://localhost:3000
PROMETHEUS_URL ?= http://localhost:9090
LOKI_URL ?= http://localhost:3100
TEMPO_URL ?= http://localhost:3200
PGADMIN_URL ?= http://localhost:5050
KAFKA_UI_URL ?= http://localhost:8081
MINIO_CONSOLE_URL ?= http://localhost:9001
MAILPIT_URL ?= http://localhost:8025

.PHONY: help up down smoke logs urls

help:
	@echo "Targets: up | down | smoke | logs | urls"

up:
	@echo "Starting E-commerce System Design Lab (placeholder)."
	@echo "Next: wire infra/services in infra/ and services/."

down:
	@echo "Stopping E-commerce System Design Lab (placeholder)."

smoke:
	@echo "Smoke checks (placeholder)."
	@echo "API: $(API_BASE_URL)"

logs:
	@echo "Logs (placeholder). Add log aggregation in infra/."

urls:
	@echo "URLs"
	@echo "- Web: $(WEB_BASE_URL)"
	@echo "- Admin: $(ADMIN_UI_URL)"
	@echo "- API: $(API_BASE_URL)"
	@echo "- Grafana: $(GRAFANA_URL)"
	@echo "- Prometheus: $(PROMETHEUS_URL)"
	@echo "- Loki: $(LOKI_URL)"
	@echo "- Tempo: $(TEMPO_URL)"
	@echo "- PgAdmin: $(PGADMIN_URL)"
	@echo "- Kafka UI: $(KAFKA_UI_URL)"
	@echo "- MinIO Console: $(MINIO_CONSOLE_URL)"
	@echo "- Mailpit: $(MAILPIT_URL)"
