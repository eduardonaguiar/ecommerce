# Platform Dependencies (Kubernetes)

Platform dependencies are deployed via ArgoCD Applications that use Helm charts with pinned versions and committed values.

## Components
- Kafka (Bitnami)
- RabbitMQ (Bitnami)
- Redis (Bitnami)
- PostgreSQL (Bitnami)
- MongoDB (Bitnami)
- Elasticsearch (Bitnami)
- Kibana (Bitnami)
- Prometheus + Grafana (kube-prometheus-stack)
- Jaeger (jaegertracing/jaeger)
- OpenTelemetry Collector (open-telemetry)

## Storage
All stateful components enable PVCs with small sizes suitable for local clusters.

## Resource Limits
Values files set conservative CPU/memory requests and limits for laptop-friendly operation.

## Notes
- Update values files to align with local storage classes if needed.
- Compose remains canonical; Kubernetes resources are additive.
