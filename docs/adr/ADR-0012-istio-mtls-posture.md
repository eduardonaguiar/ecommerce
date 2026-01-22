# ADR-0012: Istio mTLS Posture

## Status
Accepted

## Context
STOP POINT: decide mTLS posture for local mesh behavior.

## Options Considered
1. **PERMISSIVE**
   - Pros: reduces friction with local tooling and platform dependencies.
   - Cons: not strict security.
2. **STRICT**
   - Pros: production-aligned security posture.
   - Cons: more brittle locally; requires all dependencies to be mesh-aware.

## Decision
Use **PERMISSIVE** mTLS in local Kubernetes.

## Rationale
Local labs benefit from lower friction while still exercising Istio routing.

## Consequences
- mTLS is not enforced for every workload.
- The policy can be tightened in future iterations.
