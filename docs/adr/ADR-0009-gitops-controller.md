# ADR-0009: GitOps Controller

## Status
Accepted

## Context
STOP POINT: select a GitOps controller for local Kubernetes reconciliation.

## Options Considered
1. **ArgoCD**
   - Pros: mature UI, app-of-apps pattern, strong community.
   - Cons: additional UI/CLI dependency.
2. **Flux**
   - Pros: Git-native CRDs, strong Kustomize support.
   - Cons: steeper learning curve for app-of-apps workflows.

## Decision
Use **ArgoCD**.

## Rationale
ArgoCD aligns with the requested app-of-apps structure and provides a clear UX for local demos.

## Consequences
- ArgoCD CLI is required for local verification steps.
- AppProjects and Applications are committed under `k8s/gitops`.
