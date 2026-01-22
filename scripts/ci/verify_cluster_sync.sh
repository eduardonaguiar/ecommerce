#!/usr/bin/env bash
set -euo pipefail

if ! command -v argocd >/dev/null 2>&1; then
  echo "argocd CLI is required to verify sync." >&2
  exit 1
fi

argocd app sync platform-stack
argocd app sync apps-stack
argocd app wait platform-stack --health --timeout 600
argocd app wait apps-stack --health --timeout 600
