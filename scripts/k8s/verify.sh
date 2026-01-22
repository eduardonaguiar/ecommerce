#!/usr/bin/env bash
set -euo pipefail

kubectl get namespaces
kubectl get pods -n apps
kubectl get pods -n apps-notifications
kubectl get pods -n platform
kubectl get pods -n observability
kubectl get gateway -n istio-system
