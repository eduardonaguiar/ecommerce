# Jenkins CI/CD (Docker Compose)

Jenkins runs in Docker Compose and orchestrates image builds, pushes to the local registry, GitOps manifest updates, and ArgoCD reconciliation.

## Start Jenkins + Registry
```bash
docker compose -f infra/jenkins/docker-compose.jenkins.yml up -d
```

## Pipeline Responsibilities
- Build images once and tag them with `IMAGE_TAG`.
- Push images to the local registry (`localhost:5000`).
- Update GitOps manifests in `k8s/apps`.
- Trigger ArgoCD sync and wait for health.

## Jenkinsfile
The pipeline is defined in `Jenkinsfile` and invokes the scripts in `scripts/ci`.

## Credentials
- Use a local Git credential helper if pushing to a remote Git repository.
- For local-only usage, you can run with a local Git remote.

## Local Registry
- Registry is started by the Jenkins compose file on port `5000`.
- Kubernetes uses `localhost:5000` images (update manifests if your registry host differs).
