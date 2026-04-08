# RT-FDE — Tradeoffs

Decisions that were considered and explicitly rejected. Every entry here protects against relitigating the same debate.

---

## ML Model Instead of Rules (V1)

**Considered:** Train a gradient boosting or neural model on historical fraud data; replace the rule engine entirely at launch.

**Rejected because:**
- No labelled training data exists at project start.
- ML models are opaque — violates the explainability requirement.
- Rule engine provides deterministic, auditable decisions from day one.
- ML is planned for V2 as an *additive* signal, not a replacement.

**Decision:** Rules-first. ML plugs in alongside the rule engine in V2 without disrupting the scoring contract.

---

## Event Sourcing as Primary Store

**Considered:** Model all decisions as an event stream (Kafka or EventStoreDB); derive state by replaying events.

**Rejected because:**
- Adds significant operational complexity for a team not already running an event sourcing system.
- The audit requirement is satisfied by a simple append-only SQL table.
- Replay for dispute resolution can be done by querying `FraudDecisions` directly.
- Sub-200ms latency is harder to guarantee with event sourcing overhead in the synchronous path.

**Decision:** Append-only SQL table for audit. Service Bus for async event fan-out.

---

## Kafka Instead of Azure Service Bus

**Considered:** Use Kafka for async event delivery; higher throughput ceiling, log compaction.

**Rejected because:**
- Project is Azure-native; Service Bus is managed, zero-ops.
- Current throughput (1000 TPS) is well within Service Bus limits.
- Kafka requires dedicated ops investment (partitions, consumer groups, retention tuning).
- Service Bus dead-letter queue is sufficient for failure handling.

**Decision:** Service Bus for now. Revisit if throughput exceeds 5,000 TPS or multi-region fan-out is required.

---

## Fail OPEN on Timeout

**Considered:** Default the system to allowing payment through if the Fraud Engine times out, prioritising transaction completion over fraud blocking.

**Rejected as a hardcoded default because:**
- The risk tolerance for fail-open vs fail-closed is a business decision, not a technical one.
- High-risk merchant categories must fail CLOSED.
- Low-value transactions may be acceptable to fail OPEN.

**Decision:** Fail behaviour is **configuration-driven per environment/merchant**. Default in production is fail CLOSED. Operators override explicitly.

---

## Storing Full Transaction Data in Audit Log

**Considered:** Write the complete transaction payload (amount, IP, deviceId, etc.) to the audit log for maximum debuggability.

**Rejected because:**
- POPIA mandates PII minimisation. IP addresses and device fingerprints are personal data.
- Audit log records the *decision context* (score, reasons, timestamp) not the full payload.
- The originating Payment API retains full transaction data under its own retention policy.

**Decision:** Audit log stores `transactionId`, `riskScore`, `decision`, `reasons`, `timestamp` only. Sensitive fields are not duplicated.

---

## In-Process Rule Evaluation vs External Rules Service

**Considered:** Build rules as a separate microservice so rules can be updated independently of the Fraud Engine.

**Rejected because:**
- An out-of-process rules call adds a network hop to the synchronous path — directly attacks the <200ms SLA.
- Rules are loaded from DB at startup and cached in memory with a hot-reload mechanism.
- A separate service for rules is premature optimisation for V1 team size.

**Decision:** Rules evaluated in-process. Hot-reload from `RuleDefinitions` table without restart. Revisit if rules team scales independently.

---

## NoSQL (CosmosDB / MongoDB) for Audit Storage

**Considered:** Use a document store for flexible schema and scale.

**Rejected because:**
- Audit data is fixed schema — there's no benefit to document flexibility.
- SQL Server provides ACID guarantees and is already in the stack.
- Compliance teams understand SQL; queryability matters for dispute resolution.

**Decision:** SQL Server. Schema is append-only and does not change without a migration.
