# ADR-0008: Local Kubernetes Flavor

## Status
Accepted

## Context
STOP POINT: choose local Kubernetes flavor for the lab that is lightweight, repeatable, and compatible with local Docker-based workflows.

## Options Considered
1. **k3d**
   - Pros: fast startup, Docker-native, easy local registry integration.
   - Cons: extra dependency.
2. **kind**
   - Pros: widely used in CI, Kubernetes upstream alignment.
   - Cons: registry setup is more manual.
3. **minikube**
   - Pros: feature-rich, easy add-ons.
   - Cons: heavier footprint and more moving parts.

## Decision
Use **k3d** for the local Kubernetes cluster.

## Rationale
k3d provides the quickest bootstrap and simplest local registry wiring, which aligns with the Jenkins image pipeline.

## Consequences
- Developers must install k3d.
- The bootstrap script wires the local registry into the cluster.
