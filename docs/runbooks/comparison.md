# Compose vs Kubernetes Comparison

Docker Compose is canonical. Kubernetes is additive and production-like. Divergences are documented here.

## Known Differences
- **Image registry**: Compose builds locally; Kubernetes pulls from a local registry (`localhost:5000`) populated by Jenkins.
- **Ingress**: Compose exposes gateway on localhost ports; Kubernetes uses Istio ingress with `gateway.local` host.
- **mTLS**: Kubernetes uses Istio with PERMISSIVE mTLS for local stability.
- **Storage**: Kubernetes uses PVCs with default storage class; Compose uses Docker volumes.

## Alignment Goals
- Environment variables and service topology mirror Compose.
- Observability components exist in both modes.
- Any future divergence must be documented here.
