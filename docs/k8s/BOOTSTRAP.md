# Local Kubernetes Bootstrap

This guide bootstraps a **local** Kubernetes cluster in a production-like way without impacting Docker Compose usage.
Compose remains canonical; Kubernetes is additive and GitOps-managed.

## Prerequisites
- Docker Desktop or compatible Docker runtime
- `kubectl`, `helm`, and `argocd` CLI installed
- `k3d` installed (selected local cluster flavor)

## Bootstrap Steps
1. Create the local cluster and registry:
   ```bash
   scripts/k8s/bootstrap.sh
   ```
2. Install GitOps and Istio:
   - ArgoCD is installed by the bootstrap script.
   - Istio is installed via the `k8s/istio/install` manifests.
3. Connect to ArgoCD and verify sync:
   ```bash
   argocd login localhost:8081
   argocd app list
   ```

## Notes
- The cluster is intentionally sized for local laptops.
- All application and platform deployments are reconciled from Git (ArgoCD).
- Docker Compose continues to be the default local run mode.
