#!/usr/bin/env bash
set -euo pipefail

CLUSTER_NAME=${CLUSTER_NAME:-ecommerce-lab}
REGISTRY_NAME=${REGISTRY_NAME:-ecommerce-registry}
REGISTRY_PORT=${REGISTRY_PORT:-5000}
ARGOCD_VERSION=${ARGOCD_VERSION:-v2.10.5}
ISTIO_VERSION=${ISTIO_VERSION:-1.22.1}

if ! command -v k3d >/dev/null 2>&1; then
  echo "k3d is required but not installed." >&2
  exit 1
fi

if ! command -v kubectl >/dev/null 2>&1; then
  echo "kubectl is required but not installed." >&2
  exit 1
fi

if ! command -v helm >/dev/null 2>&1; then
  echo "helm is required but not installed." >&2
  exit 1
fi

if ! k3d registry list | grep -q "${REGISTRY_NAME}"; then
  k3d registry create "${REGISTRY_NAME}" --port "${REGISTRY_PORT}"
fi

if ! k3d cluster list | grep -q "${CLUSTER_NAME}"; then
  k3d cluster create "${CLUSTER_NAME}" \
    --agents 2 \
    --servers 1 \
    --registry-use "k3d-${REGISTRY_NAME}:${REGISTRY_PORT}" \
    --port "8081:80@loadbalancer" \
    --k3s-arg "--disable=traefik@server:0"
fi

kubectl create namespace argocd --dry-run=client -o yaml | kubectl apply -f -

kubectl apply -n argocd -f "https://raw.githubusercontent.com/argoproj/argo-cd/${ARGOCD_VERSION}/manifests/install.yaml"

kubectl apply -f k8s/gitops/projects/platform.yaml
kubectl apply -f k8s/gitops/projects/apps.yaml
kubectl apply -f k8s/gitops/apps/app-of-apps.yaml

kubectl apply -f k8s/istio/install/istio-operator.yaml
kubectl apply -f k8s/istio/install/istio-base.yaml
kubectl apply -f k8s/istio/install/istio-ingress-gateway.yaml

kubectl -n istio-system rollout status deployment/istiod --timeout=120s

cat <<INFO
Bootstrap complete.
- Cluster: ${CLUSTER_NAME}
- Registry: k3d-${REGISTRY_NAME}:${REGISTRY_PORT}
- ArgoCD namespace: argocd
- Istio version: ${ISTIO_VERSION}
INFO
