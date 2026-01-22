# GitOps (ArgoCD)

Kubernetes deployments are reconciled from Git using ArgoCD. No manual `kubectl apply` is used for application lifecycle beyond bootstrapping.

## Structure
- `k8s/gitops/projects`: ArgoCD AppProjects for platform and apps.
- `k8s/gitops/apps`: App-of-Apps pointing at platform and app stacks.

## Bootstrap
- `scripts/k8s/bootstrap.sh` installs ArgoCD and applies the AppProjects + App-of-Apps.
- Update `repoURL` entries to match your local Git remote.

## Workflow
1. Jenkins builds and pushes images to the local registry.
2. Jenkins updates GitOps manifests with the new image tags.
3. ArgoCD reconciles the desired state automatically.

## Divergences
All Kubernetes-specific adjustments (e.g., storage classes, resource limits) are documented in `docs/runbooks/comparison.md`.
