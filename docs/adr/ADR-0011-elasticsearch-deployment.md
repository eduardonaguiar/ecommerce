# ADR-0011: Elasticsearch Deployment Style

## Status
Accepted

## Context
STOP POINT: choose Elasticsearch deployment approach suitable for local Kubernetes.

## Options Considered
1. **Bitnami Elasticsearch Helm chart (single node)**
   - Pros: easy to operate locally, minimal resource footprint.
   - Cons: not production HA.
2. **Elastic Cloud on Kubernetes (ECK)**
   - Pros: operator-based lifecycle, production-grade.
   - Cons: heavier for local use and adds CRDs.

## Decision
Use **Bitnami Elasticsearch Helm chart** in single-node mode.

## Rationale
The lab prioritizes local reproducibility and resource efficiency.

## Consequences
- Single-node topology is not HA.
- Values are pinned in `k8s/platform/elasticsearch/values.yaml`.
