pipeline {
  agent any
  environment {
    REGISTRY = "${REGISTRY ?: 'localhost:5000'}"
    IMAGE_TAG = "${IMAGE_TAG ?: 'dev-${BUILD_NUMBER}'}"
  }
  stages {
    stage('Build Images') {
      steps {
        sh 'scripts/ci/build_images.sh'
      }
    }
    stage('Push Images') {
      steps {
        sh 'scripts/ci/push_images.sh'
      }
    }
    stage('Update GitOps Manifests') {
      steps {
        sh 'scripts/ci/update_gitops_images.sh'
        sh 'git status --porcelain'
      }
    }
    stage('Commit GitOps Changes') {
      steps {
        sh 'git config user.email "jenkins@local"'
        sh 'git config user.name "Jenkins"'
        sh 'git add k8s/apps'
        sh 'git commit -m "chore(gitops): update image tags" || true'
      }
    }
    stage('Verify ArgoCD Sync') {
      steps {
        sh 'scripts/ci/verify_cluster_sync.sh'
      }
    }
  }
}
