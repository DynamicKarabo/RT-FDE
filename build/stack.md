# RT-FDE — Tech Stack

Every choice below is binding until overturned in `decision-log.md`.

---

## Core Stack

| Layer | Technology | Rationale |
|-------|-----------|-----------|
| API runtime | .NET 8 (ASP.NET Core) | Low-latency, strongly typed; native async; excellent Azure integration |
| Behaviour store | Redis | Sub-millisecond key lookups; TTL-native; required for velocity tracking at 1000+ TPS |
| Primary database | SQL Server | Append-only audit writes; structured rule storage; ACID for compliance |
| Message bus | Azure Service Bus | Durable, ordered delivery; dead-letter queue built-in; native Azure ecosystem |
| Rule storage | SQL Server table `RuleDefinitions` | Rules must be auditable and hot-reloadable without a deploy |
| Hosting | Azure (AKS or App Service) | Co-located with Service Bus; managed TLS; horizontal scale |

---

## Versioning & API

| Concern | Choice | Rationale |
|---------|--------|-----------|
| API versioning | URL-based (`/v1/fraud/evaluate`) | Explicit; no header negotiation complexity |
| Serialisation | `System.Text.Json` | Zero-dependency; fast; built into .NET 8 |
| Contract validation | FluentValidation | Declarative; testable; separates validation from controller logic |

---

## Observability

| Concern | Choice | Rationale |
|---------|--------|-----------|
| Structured logging | Serilog → Azure Monitor | Queryable JSON logs; correlation IDs per transaction |
| Tracing | OpenTelemetry | Vendor-neutral; traces the full synchronous path |
| Metrics | Prometheus + Grafana (or Azure Monitor) | Latency histograms, TPS counters, decision distribution |
| Alerting | Azure Monitor Alerts | Threshold-based; PagerDuty-compatible |

---

## Testing

| Layer | Tool |
|-------|------|
| Unit | xUnit |
| Integration | xUnit + Testcontainers (Redis, SQL Server) |
| Contract | Pact (if Payment API is separately owned) |
| Load | k6 — validates 1000+ TPS target |

---

## CI/CD

| Stage | Tool |
|-------|------|
| Pipeline | GitHub Actions |
| Image build | Docker (multi-stage) |
| Deploy | Helm chart to AKS |
| Secrets | Azure Key Vault — no secrets in repo or env vars committed to source control |

---

## Language & Runtime Constraints

- Target: **.NET 8 LTS**
- C# 12
- Nullable reference types **enabled** globally
- `async`/`await` throughout — no `Task.Result` or `.Wait()` anywhere
