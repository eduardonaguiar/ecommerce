# ADR-0010: Kafka Runtime

## Status
Accepted

## Context
STOP POINT: choose Kafka runtime and deployment style for local Kubernetes.

## Options Considered
1. **Bitnami Kafka Helm chart (KRaft)**
   - Pros: straightforward Helm install, single-node friendly.
   - Cons: not operator-based.
2. **Strimzi operator**
   - Pros: operator-driven lifecycle, production-aligned CRDs.
   - Cons: heavier footprint for local lab.

## Decision
Use **Bitnami Kafka Helm chart** with KRaft enabled.

## Rationale
The Helm chart is lightweight and well suited for local clusters while preserving Kafka semantics.

## Consequences
- No Zookeeper in Kubernetes (KRaft mode).
- Values are pinned in `k8s/platform/kafka/values.yaml`.
