# Non-Functional Requirements (NFR) — Canonical List

Rules:
- Each NFR is atomic, preferably verifiable locally.
- If a target number is missing, it must remain qualitative or be flagged as a STOP.
- Focus on study value: observability, reproducibility, failure modes.

| NFR-ID | Category | Requirement | Applies To | Verification | Risk Addressed |
|---|---|---|---|---|---|
| NFR-ECOM-001 | Operability | The system shall be runnable via Docker Compose with a single command. | All | `docker compose up -d` | Setup friction |
| NFR-ECOM-002 | Operability | Compose shall include healthchecks and explicit dependency wiring. | All | `docker ps`, health status | Flaky startup |
| NFR-ECOM-003 | Observability | Logs shall be structured JSON and centrally searchable in Kibana. | All | Kibana query by service | Debug blindness |
| NFR-ECOM-004 | Observability | Metrics shall be scraped by Prometheus and visible in Grafana. | All | Prometheus targets UP | No quantitative signals |
| NFR-ECOM-005 | Observability | Distributed tracing shall show end-to-end paths in Jaeger. | HTTP + messaging services | Jaeger trace spans across services | No causality |
| NFR-ECOM-006 | Observability | Correlation IDs shall propagate through HTTP and messaging. | All | Logs/traces contain correlationId | Hard incident triage |
| NFR-ECOM-007 | Performance | Catalog and Query read paths shall support caching to reduce DB load. | Catalog/Query | Cache hit/miss metrics | Read amplification |
| NFR-ECOM-008 | Scalability | Services shall be stateless at API layer to allow horizontal scaling. | All APIs | Multiple replicas possible in compose | Scaling bottlenecks |
| NFR-ECOM-009 | Performance | Redis cache shall expose hit/miss and latency metrics. | Query/Catalog | Grafana dashboard | Opaque caching |
| NFR-ECOM-010 | CQRS | Read models shall be rebuildable from events (non-authoritative). | Query | Rebuild runbook works | Corrupted views |
| NFR-ECOM-011 | Consistency | Cross-service state shall be eventually consistent; local DB remains authoritative per service. | All | Documented + observed lag scenarios | Wrong assumptions |
| NFR-ECOM-012 | Data Integrity | Orders/Payments/Inventory shall use ACID local transactions. | Orders/Payments/Inventory | DB constraints + tests | Partial local writes |
| NFR-ECOM-013 | Reliability | Event consumption shall tolerate at-least-once delivery via idempotency/dedup. | Kafka/Rabbit consumers | Duplicate delivery playbook | Double side-effects |
| NFR-ECOM-014 | Resilience | Retries must use backoff and have bounded attempts. | HTTP + consumers | Logs show retry policy | Retry storms |
| NFR-ECOM-015 | Resilience | Timeouts must be set for outbound calls (HTTP/broker). | Services calling deps | Config review + tests | Resource exhaustion |
| NFR-ECOM-016 | Backpressure | System shall support backpressure observation via queue depth/consumer lag metrics. | Kafka/Rabbit | Grafana lag/queue depth | Hidden overload |
| NFR-ECOM-017 | Reliability | Order finalization shall be deterministic given the same event history. | Orders | Reprocess events yields same status | Non-determinism |
| NFR-ECOM-018 | Reliability | Notification failures shall be isolated and never affect order correctness. | Notifications | Simulate worker down | Cascading failures |
| NFR-ECOM-019 | Security | JWT validation rules must be explicit and enforced at gateway. | Gateway | 401/403 tests | Unauthorized access |
| NFR-ECOM-020 | Security | TLS must be enabled at gateway for external traffic (dev). | Gateway | HTTPS call succeeds | Cleartext traffic |
| NFR-ECOM-021 | Security | Secrets shall not be committed; env conventions must be documented. | Repo | repo scan | Credential leaks |
| NFR-ECOM-022 | Privacy | Logs must avoid direct PII; redact where needed. | All | log review + tests | PII leakage |
| NFR-ECOM-023 | Security | Sensitive data at rest encryption approach must be documented (STOP if undecided). | DB services | ADR exists | Data exposure |
| NFR-ECOM-024 | Maintainability | Services shall follow consistent folder structure and naming. | All | repo structure | Agent confusion |
| NFR-ECOM-025 | Maintainability | APIs and events shall be versioned (at least via version fields). | All | envelope includes version | Breaking changes |
| NFR-ECOM-026 | Testability | A smoke test suite shall validate basic system health and routes. | System | `make smoke` passes | Regression undetected |
| NFR-ECOM-027 | Resilience | Fault injection scenarios shall be documented and reproducible. | System | chaos scripts | No practice of failure modes |
| NFR-ECOM-028 | DevEx | Developer should reach a working environment in <= 3 commands. | Repo | quickstart | Slow onboarding |
| NFR-ECOM-029 | Documentation | Architecture, events, and decisions shall be documented as first-class artifacts. | Docs | docs complete | Semantic drift |
| NFR-ECOM-030 | Operability | Port map and service registry shall be documented and kept consistent. | Infra/Docs | docs vs compose | Confusion |

STOP-sensitive NFRs:
- Retention periods (logs/traces/metrics)
- Sampling policy for traces
- Encryption at rest scope/strategy
- Concrete SLO numbers (optional for lab)
