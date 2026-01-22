# ADR-0007: Telemetry Sampling and Retention

## Status
Accepted

## Context
The lab requires observable logs, metrics, and traces. STOP-8 asks for a decision on trace sampling and retention to balance volume with usefulness. In a local lab, cost is less critical than visibility.

## Options Considered
1. **100% trace sampling with short retention**
   - Pros: Maximum visibility for learners; easiest debugging; no sampling bias.
   - Cons: Higher local storage/CPU usage (acceptable for short retention).
2. **Sampled traces with longer retention (e.g., 10-20%)**
   - Pros: Lower volume and longer history.
   - Cons: Harder to guarantee visibility of specific test flows.

## Decision
**Use 100% trace sampling with short retention (e.g., 3 days) for traces; logs/metrics can keep a similar short window.**

## Rationale
Full sampling accelerates the lab by ensuring every test run is observable without extra configuration or guessing whether a trace was captured. Short retention limits local resource usage while keeping recent runs available.

## Consequences
- Storage needs remain modest due to short retention windows.
- Debugging is straightforward because all traces are captured.
- Sampling can be introduced later if volume becomes problematic.
