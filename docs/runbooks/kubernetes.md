# Runbook: Kubernetes (Local)

Kubernetes is additive and GitOps-managed via ArgoCD.

## Bootstrap
```bash
scripts/k8s/bootstrap.sh
```

## Verify ArgoCD
```bash
argocd app list
argocd app get apps-stack
```

## Istio Ingress
Add the host entry:
```bash
sudo -- sh -c 'echo "127.0.0.1 gateway.local" >> /etc/hosts'
```
Then access:
- Gateway: http://gateway.local

## Observability
- Grafana (observability namespace): `kubectl port-forward svc/observability-grafana 3000:80 -n observability`
- Jaeger: `kubectl port-forward svc/jaeger-query 16686:16686 -n observability`

## Troubleshooting
- Check pod health: `kubectl get pods -A`
- ArgoCD sync: `argocd app sync apps-stack`
