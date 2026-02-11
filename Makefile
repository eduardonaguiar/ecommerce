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

.PHONY: help up down smoke logs urls e2e e2e-saga-success e2e-saga-failure-payment e2e-saga-failure-stock e2e-idempotency e2e-cqrs e2e-security e2e-functional e2e-functional-success e2e-functional-failure e2e-functional-cqrs e2e-functional-cart e2e-functional-notifications jwt-dev

COMPOSE_FILE ?= infra/compose/docker-compose.yml
DOCKER_COMPOSE ?= docker compose

help:
	@echo "Targets: up | down | smoke | logs | urls"

up:
	@$(DOCKER_COMPOSE) -f $(COMPOSE_FILE) up -d

down:
	@$(DOCKER_COMPOSE) -f $(COMPOSE_FILE) down

smoke:
	@scripts/smoke/smoke.sh

K6_IMAGE ?= grafana/k6:0.49.0
K6_DOCKER_FLAGS ?= --network host
K6_RUN = docker run --rm -i $(K6_DOCKER_FLAGS) -v $(PWD)/tests/k6:/tests -w /tests $(K6_IMAGE) run

e2e: e2e-saga-success e2e-saga-failure-payment e2e-saga-failure-stock e2e-idempotency e2e-cqrs e2e-security

e2e-saga-success:
	@$(K6_RUN) /tests/e2e_saga_success.js

e2e-saga-failure-payment:
	@$(K6_RUN) /tests/e2e_saga_payment_failure.js

e2e-saga-failure-stock:
	@$(K6_RUN) /tests/e2e_saga_stock_failure.js

e2e-idempotency:
	@$(K6_RUN) /tests/e2e_idempotency_duplicates.js

e2e-cqrs:
	@$(K6_RUN) /tests/e2e_cqrs_convergence.js

e2e-security:
	@$(K6_RUN) /tests/e2e_security_tls.js

e2e-functional: e2e-functional-success e2e-functional-failure e2e-functional-cart e2e-functional-cqrs e2e-functional-notifications

e2e-functional-success:
	@$(K6_RUN) /tests/functional/purchase_success.js

e2e-functional-failure:
	@$(K6_RUN) /tests/functional/purchase_payment_failure.js
	@$(K6_RUN) /tests/functional/purchase_out_of_stock.js

e2e-functional-cart:
	@$(K6_RUN) /tests/functional/cart_semantics.js

e2e-functional-cqrs:
	@$(K6_RUN) /tests/functional/cqrs_eventual_consistency.js

e2e-functional-notifications:
	@$(K6_RUN) /tests/functional/notification_side_effects.js

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


jwt-dev:
	@if [ -z "$$JWT_HS256_SECRET" ]; then \
		echo "JWT_HS256_SECRET is required. Example: JWT_HS256_SECRET=dev-secret make jwt-dev"; \
		exit 1; \
	fi
	@python3 scripts/testdata/generate_dev_jwt.py --secret "$$JWT_HS256_SECRET"
