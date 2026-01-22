# Istio Service Mesh

Istio is required for all Kubernetes app-to-app traffic. The mesh is enabled via namespace labels and sidecar injection.

## Installation
- Control plane: `k8s/istio/install/istio-operator.yaml`
- Ingress gateway: `k8s/istio/install/istio-ingress-gateway.yaml`
- Namespaces: `k8s/istio/install/istio-base.yaml`

## Traffic and Policies
- mTLS policy: `k8s/istio/policies/mtls.yaml` (PERMISSIVE for local compatibility)
- Default destination rule: `k8s/istio/traffic/defaults.yaml`
- Ingress routing: `k8s/istio/ingress/gateway.yaml`

## Notes
- The gateway is exposed via Istio ingress and reachable at `http://gateway.local` after hosts file entry.
- mTLS is PERMISSIVE for local environment stability; see ADR for posture decision.
