#!/usr/bin/env bash
set -euo pipefail

REGISTRY=${REGISTRY:-localhost:5000}
TAG=${IMAGE_TAG:-latest}

mapfile -t files < <(rg --files -g 'deployment.yaml' k8s/apps)

for file in "${files[@]}"; do
  perl -pi -e "s#image: .*?/ecommerce-([a-z-]+):\S+#image: ${REGISTRY}/ecommerce-$1:${TAG}#" "${file}"
done
