# RT-FDE — Infrastructure Standards

---

## Environments

| Environment | Purpose | Notes |
|-------------|---------|-------|
| `local` | Developer machines | Docker Compose; Redis + SQL Server in containers |
| `dev` | Continuous integration | Deployed on every merge to `main` |
| `staging` | Pre-production validation | Production parity; used for load tests |
| `production` | Live traffic | Fail CLOSED on timeout; full audit enabled |

Environment is injected via `ASPNETCORE_ENVIRONMENT`. Application code must never branch on environment name — use configuration values instead.

---

## Configuration Rules

**No secrets in source control.** Ever. Not even in `.gitignore`d files.

| Config type | Source |
|-------------|--------|
| Secrets (connection strings, signing keys) | Azure Key Vault |
| Environment-specific values | Azure App Configuration |
| Feature flags | Azure App Configuration |
| Default/non-sensitive config | `appsettings.json` committed to repo |

All configuration is strongly typed and bound at startup. The application fails fast on missing required config — no silent nulls at runtime.

```csharp
// All threshold values come from config
services.Configure<FraudThresholds>(configuration.GetSection("FraudThresholds"));
```

**Threshold values are not hardcoded.** `REVIEW_THRESHOLD`, `REJECT_THRESHOLD`, and all rule weights are configurable without a code deploy.

---

## Redis

- Keys always include a TTL. No TTL = deployment blocker.
- Key format: `noun:{id}:descriptor:window` (see `coding-standards.md`).
- Connection resilience: Polly retry with exponential backoff; max 2 retries before degraded-mode fallback.
- Redis is a cache, not a database. Data loss on restart is acceptable.

---

## SQL Server

- All schema changes delivered via migration scripts (Flyway or EF Core migrations — one tool, not both).
- `FraudDecisions` table: no `UPDATE`, no `DELETE` permissions granted to the application service account.
- Application service account has minimum required permissions: `INSERT` on `FraudDecisions`, `SELECT`/`INSERT`/`UPDATE` on `RuleDefinitions`.
- Connection pooling via `SqlConnection` pool defaults; max pool size reviewed at staging load test.

---

## Azure Service Bus

- One topic per event type: `fraud-evaluated`, `fraud-rejected`, `fraud-flagged-for-review`.
- All messages include `transactionId` in the message properties (not just the body) for dead-letter inspection.
- Dead-letter queue alerting is configured — unprocessed DLQ messages trigger an alert within 5 minutes.
- Message TTL: 7 days.

---

## Deployment

- Containerised via Docker. Multi-stage build: SDK image for build, runtime image for deploy.
- No `root` user in the container.
- Health checks:
  - `/health/live` — process is alive
  - `/health/ready` — Redis and SQL Server are reachable
- Kubernetes readiness probe uses `/health/ready`. Liveness probe uses `/health/live`.
- Rollout strategy: rolling update with `maxUnavailable: 0` — zero downtime deploys.
- Horizontal Pod Autoscaler configured on CPU and request latency metrics.

---

## Secrets Rotation

- All secrets reference Azure Key Vault by name + version.
- Rotation does not require a redeploy — App Service / AKS pulls updated secrets on pod restart or via Key Vault references with refresh intervals.

---

## Encryption

- All data in transit: TLS 1.2+ enforced. No HTTP.
- Sensitive fields at rest (IP address, device fingerprints in `RuleDefinitions` if stored): encrypted via SQL Server TDE + column-level encryption for PII-adjacent fields.
- Internal Service Bus events are HMAC-signed. Consumers verify signature before processing.

---

## Monitoring & Alerting

| Signal | Alert threshold | Owner |
|--------|----------------|-------|
| P99 latency > 200ms | Immediate | On-call |
| Error rate > 1% | Immediate | On-call |
| Redis connection failures | >3 in 60s | On-call |
| Audit write failures | Any | On-call |
| DLQ depth > 0 | Within 5 min | On-call |
| Fraud rejection rate spike (>2× baseline) | Within 10 min | Fraud ops |

Dashboards live in Azure Monitor / Grafana. Dashboard-as-code (Terraform or Bicep) — no manual dashboard configuration.

---

## Access Control

- Application identity uses a **Managed Identity** — no stored credentials.
- Dashboard access is RBAC-gated. Read-only role for ops. Write (rule editing) for fraud analysts only.
- No direct production database access for developers. All access via approved runbook procedures with audit trail.
