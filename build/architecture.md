# RT-FDE — Architecture

## Project Overview

**RT-FDE** (Real-Time Fraud Detection Engine) is a synchronous risk assessment service that intercepts payment transactions at the **Authorised → Captured** transition and returns a scored, explainable decision before capture is allowed to proceed.

**Who it's for:** Payment platforms that need sub-200ms fraud verdicts with full compliance auditability (POPIA-aligned, ZAR-first).

**Core value proposition:** Every financial decision is *explainable*, *consistent*, and *safe under failure* — not just "fraud is blocked".

---

## Goals

| Must | Must Not |
|------|----------|
| Return APPROVE / REJECT / REVIEW in <200ms | Miss or skip the Authorised→Captured gate |
| Produce deterministic, reproducible decisions | Introduce duplicate evaluations for the same `transactionId` |
| Write an immutable audit record for every decision | Store unnecessary PII beyond what scoring requires |
| Degrade gracefully when Redis or downstream services fail | Block a payment indefinitely due to internal timeouts |
| Support 1000+ TPS with horizontal scale | Make irreversible decisions without an audit trail |

---

## System Context

```
Client
  └─► Payment API
        └─► Fraud Engine  ◄── Redis (behaviour store)
              │                └─► SQL Server (rules + audit)
              │                └─► Azure Service Bus (events)
              ▼
        Decision (APPROVE / REJECT / REVIEW)
              │
        Payment Processor (capture allowed only on APPROVE)
```

The Fraud Engine is a **synchronous blocker** in the payment critical path. Every other concern (analytics, alerting, dashboard) is decoupled via async events.

---

## Components

### 1. Fraud API — Synchronous Layer

**Responsibility:** Accept a transaction, orchestrate evaluation, return decision.

```
POST /fraud/evaluate
```

**Request contract:**
```json
{
  "transactionId": "uuid",
  "userId":        "uuid",
  "amount":        2500.00,
  "currency":      "ZAR",
  "timestamp":     "ISO8601",
  "ipAddress":     "string",
  "deviceId":      "string",
  "merchantId":    "string"
}
```

**Response contract:**
```json
{
  "decision":  "APPROVE | REJECT | REVIEW",
  "riskScore": 0-100,
  "reasons":   ["HIGH_AMOUNT_ANOMALY", "NEW_DEVICE", "HIGH_VELOCITY"]
}
```

**Idempotency:** If `transactionId` already has a persisted decision, return it immediately — do not re-evaluate.

---

### 2. Rule Engine

**Responsibility:** Evaluate predefined fraud signals against the transaction context.

Rules are loaded from DB/config at startup (or hot-reloaded) and evaluated entirely in memory.

| Rule | Condition | Score Delta |
|------|-----------|-------------|
| Amount anomaly | `amount > user_avg × 3` | +30 |
| Velocity breach | `txns in last 60s > 5` | +25 |
| Geo anomaly | geo distance > 1000km in <1hr | +25 |
| New device | `deviceId NOT IN known_devices` | +20 |

Rules are additive. All matching rules fire; their deltas are summed.

---

### 3. Risk Scoring Engine

**Responsibility:** Aggregate rule outputs into a single `0–100` risk score.

- V1: pure deterministic rules (sum of matched rule weights, capped at 100).
- V2+: ML model can be slotted in as an additional signal source without replacing the rule engine.

---

### 4. Decision Engine

**Responsibility:** Map risk score to a final action.

| Score | Decision |
|-------|----------|
| 0–39 | APPROVE |
| 40–69 | REVIEW |
| 70–100 | REJECT |

Thresholds are configuration-driven, not hardcoded.

---

### 5. Real-Time Behaviour Store (Redis)

**Responsibility:** Maintain ephemeral, TTL-bound behavioural state per user.

| Key | Purpose |
|-----|---------|
| `user:{id}:txn_count:1min` | Velocity tracking |
| `user:{id}:last_ip` | IP change detection |
| `user:{id}:devices` | Known device set |

All keys carry TTLs. Redis is treated as a **cache** — its absence must not prevent evaluation (see Failure Handling).

---

### 6. Audit Log

**Responsibility:** Append-only, immutable record of every decision.

```json
{
  "transactionId": "uuid",
  "riskScore":     87,
  "decision":      "REJECT",
  "reasons":       ["HIGH_VELOCITY", "NEW_DEVICE"],
  "timestamp":     "ISO8601"
}
```

Written to `FraudDecisions` table in SQL Server. No updates, no deletes. Required for compliance, debugging, and dispute resolution.

---

### 7. Async Processing Layer (Azure Service Bus)

**Responsibility:** Decouple downstream consumers from the synchronous path.

Events emitted after audit write:

| Event | Trigger |
|-------|---------|
| `FraudEvaluated` | Every evaluation |
| `FraudRejected` | Decision = REJECT |
| `FraudFlaggedForReview` | Decision = REVIEW |

Consumers: analytics pipeline, alerting system, ops dashboard. None of these are in the critical path.

---

## Data Flow

### Real-Time (Critical Path)

```
1. Payment API → POST /fraud/evaluate
2. Check idempotency (transactionId lookup)
3. Fetch behavioural context from Redis
4. Evaluate rules in memory
5. Compute risk score
6. Map score → decision
7. Return decision (<200ms SLA)
8. [Async] Persist audit log → SQL Server
9. [Async] Emit event → Service Bus
```

Steps 8 and 9 happen **after** the response is returned. They must not extend response latency.

### Async (Post-Decision)

```
Service Bus → Analytics Service
           → Alerting System
           → Dashboard
```

---

## Failure Handling

| Failure | Behaviour |
|---------|-----------|
| Redis unavailable | Evaluate without behavioural signals; score conservatively; default to REVIEW |
| Fraud Engine timeout (caller-side) | Configurable: fail CLOSED (block) or fail OPEN (allow) |
| Duplicate `transactionId` | Return cached decision; no re-evaluation |
| Audit write fails | Surface error; do not silently drop; alert on-call |

---

## Performance Targets

| Metric | Target |
|--------|--------|
| P99 latency | <200ms |
| Throughput | 1,000+ TPS |
| Availability | 99.9% |
