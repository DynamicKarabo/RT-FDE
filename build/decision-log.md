# RT-FDE — Decision Log

Every significant architectural, technical, or product decision is recorded here. Format: decision, context, rationale, alternatives considered, date.

Append-only. Do not edit past entries — add a superseding entry if a decision changes.

---

## DEC-001 — Rules-first, ML in V2

**Decision:** V1 uses a deterministic, rules-based scoring engine. ML is explicitly deferred to V2.

**Context:** System requires explainable, auditable decisions from day one. No labelled training data exists.

**Rationale:** Rules are transparent, testable, and immediately deployable. ML adds opacity and requires data that doesn't yet exist. V2 design accounts for ML as an additive signal alongside rules.

**Alternatives:** ML-only; hybrid from launch.

**Date:** 2025-01-01

---

## DEC-002 — Append-only SQL Server for audit, not event sourcing

**Decision:** `FraudDecisions` is a simple append-only SQL table. Event sourcing was rejected.

**Context:** Compliance requires an immutable, queryable audit trail. Team has no existing event sourcing infrastructure.

**Rationale:** SQL satisfies the compliance requirement with far less operational overhead. Dispute resolution queries are straightforward SQL. Event sourcing complexity is not justified by current requirements.

**Alternatives:** EventStoreDB, Kafka-as-log.

**Date:** 2025-01-01

---

## DEC-003 — Azure Service Bus over Kafka

**Decision:** Async event delivery via Azure Service Bus.

**Context:** Project is Azure-native. Current throughput target is 1000 TPS.

**Rationale:** Service Bus is fully managed, requires zero ops, and handles current scale. Kafka requires dedicated cluster management and partition planning that is not justified at this stage.

**Trigger to revisit:** Sustained throughput > 5,000 TPS or multi-region event fan-out requirement.

**Alternatives:** Kafka, RabbitMQ, Azure Event Hubs.

**Date:** 2025-01-01

---

## DEC-004 — Fail behaviour is configuration-driven, default CLOSED

**Decision:** Payment timeout behaviour (fail OPEN vs CLOSED) is configurable per environment and merchant. Production default is fail CLOSED.

**Context:** Risk tolerance for allowing a transaction through on timeout is a business decision that varies by merchant category and transaction value.

**Rationale:** Hardcoding either behaviour is wrong. Fail CLOSED is the safe default. Operators explicitly opt into fail OPEN where justified.

**Alternatives:** Always fail OPEN; always fail CLOSED.

**Date:** 2025-01-01

---

## DEC-005 — Rules evaluated in-process, not as a separate service

**Decision:** Rule engine runs in-process with the Fraud API. Rules are loaded from DB and cached in memory.

**Context:** Each out-of-process call adds latency to the synchronous critical path.

**Rationale:** A network hop to a rules service directly attacks the <200ms SLA. In-memory evaluation with DB-backed hot reload satisfies both the performance requirement and the need to update rules without a deploy.

**Trigger to revisit:** If a dedicated rules team operates independently and requires independent deployability.

**Alternatives:** Dedicated rules microservice; inline rule definitions in code.

**Date:** 2025-01-01

---

## DEC-006 — Audit log stores decision context only, not full transaction payload

**Decision:** `FraudDecisions` records `transactionId`, `riskScore`, `decision`, `reasons`, `timestamp`. No PII fields.

**Context:** POPIA mandates PII minimisation. IP addresses and device identifiers are personal data.

**Rationale:** The audit requirement is to record what decision was made and why — not to duplicate the full transaction. The source-of-truth for transaction data is the Payment API.

**Alternatives:** Store full payload; store encrypted payload.

**Date:** 2025-01-01

---

## DEC-007 — Thresholds and rule weights are configuration, not code

**Decision:** Decision thresholds (0–39 APPROVE, 40–69 REVIEW, 70–100 REJECT) and rule score weights are stored in configuration, not hardcoded.

**Context:** Fraud teams need to tune thresholds in response to changing fraud patterns without a code deployment.

**Rationale:** Codifying thresholds creates a deploy-to-tune cycle that slows fraud response. Config-driven values allow same-day tuning.

**Alternatives:** Hardcoded constants; feature flags.

**Date:** 2025-01-01

---

## DEC-008 — Idempotency enforced by transactionId lookup

**Decision:** On receipt of a `POST /fraud/evaluate`, the engine immediately checks whether a decision already exists for `transactionId`. If yes, return the stored decision without re-evaluating.

**Context:** Payment APIs may retry on timeout. Re-evaluating the same transaction could produce a different decision if behavioural state has changed.

**Rationale:** Deterministic consistency — same transaction always produces the same outcome. Prevents race conditions on concurrent retries.

**Alternatives:** Lock-based deduplication; accept re-evaluation risk.

**Date:** 2025-01-01

---

## Open Questions

Track unresolved decisions here until they are closed and moved above.

| # | Question | Owner | Due |
|---|----------|-------|-----|
| OQ-001 | What is the agreed SLA breach behaviour for merchant category X (high-value B2B)? Fail OPEN or CLOSED? | Product + Risk | Before staging |
| OQ-002 | Who owns rule definition changes in production — engineering or fraud analysts? What is the approval workflow? | Product | Before go-live |
| OQ-003 | What is the data retention period for `FraudDecisions`? Legal / compliance sign-off required. | Legal | Before go-live |
| OQ-004 | Is `UserProfiles` table in scope for V1 or deferred? Geo anomaly rule requires historical location data. | Engineering | Sprint 1 |
| OQ-005 | Hot-reload interval for rule changes — how quickly must a new rule take effect? Real-time? 60s? | Fraud ops | Sprint 2 |

---

## DEC-009 — Polly Resilience for Redis (2 retries, Exponential Backoff)

**Decision:** All Redis operations are wrapped in a Polly retry policy with a maximum of 2 retries and exponential backoff.

**Context:** Transient network blips or Redis failovers must not immediately trigger degraded mode, which reduces scoring accuracy.

**Rationale:** Protecting the 1000+ TPS throughput requires resilience against minor infrastructure fluctuations. 2 retries provide a balance between reliability and the <200ms latency SLA.

**Alternatives:** Fail fast (no retries); circuit breaker (deferred to V2).

**Date:** 2026-04-08

---

## DEC-010 — Standardized API Error Shape (RFC 7807 ProblemDetails)

**Decision:** The API uses the RFC 7807 `ProblemDetails` standard for all error responses.

**Context:** Upstream clients need a predictable, machine-readable format for handling errors (validation, internal failures, etc.).

**Rationale:** Using a standard like `ProblemDetails` ensures consistency across the platform. Ad-hoc JSON payloads for errors are banned to prevent client-side fragmentation.

**Alternatives:** Custom JSON `{ error: "msg" }`.

**Date:** 2026-04-08

---

## DEC-011 — Enum-based Rule Typing (Strict Rule Reasons)

**Decision:** Fraud rule categories are defined using a strictly typed `RuleType` enum.

**Context:** The engine previously relied on magic strings for rule identification, making the system prone to typos and refactoring errors.

**Rationale:** Enums provide compile-time safety and clear documentation of supported signals. Mapping to human-readable reasons is encapsulated in the domain model.

**Alternatives:** String constants (Magic strings).

**Date:** 2026-04-08

---

## DEC-012 — Authentic Health Probes (Redis/SQL Pings)

**Decision:** The `/health/ready` endpoint performs real asynchronous pings to Redis and SQL Server.

**Context:** Kubernetes readiness probes must reflect the actual ability of the pod to process traffic, not just that the process is running.

**Rationale:** Static health checks can lead to routing traffic to "zombie" pods that have lost database connectivity. Authentic probes ensure traffic is only routed to fully functional instances.

**Alternatives:** Static "Healthy" responses.

**Date:** 2026-04-08

