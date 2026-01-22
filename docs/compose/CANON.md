# Docker Compose Canonical Baseline

Docker Compose is the **canonical** local execution model for the E-commerce System Design Lab.
Kubernetes is additive and must never replace, simplify, or deprecate Compose.

## Why Compose is Canonical
- **Fast feedback**: one-command bring-up (`docker compose up`) for rapid iteration.
- **Debuggability**: services, brokers, and observability stacks are accessible on localhost.
- **Chaos testing**: compose-native tooling and container controls are the default.
- **E2E tests (k6)**: Compose is the reference environment for k6 and lab workflows.
- **Documentation baseline**: Compose behavior is authoritative; divergences in Kubernetes must be documented.

## Supported Workflows
- **Dev loop**: build and run with `docker compose up` from `infra/compose`.
- **Debugging**: inspect logs, exec into containers, or attach debuggers locally.
- **Chaos**: pause/kill/restart containers to simulate failures.
- **E2E**: run k6 tests against the Compose gateway service.

## Guarantees
- Every service, database, broker, and observability component remains present in Compose.
- No feature requires Kubernetes to function.
- Any Compose vs Kubernetes divergence is documented in the runbook and ADRs.
