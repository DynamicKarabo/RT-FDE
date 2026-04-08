# RT-FDE — Coding Standards

These are non-negotiable. Deviations require an entry in `decision-log.md`.

---

## Project Structure

```
src/
  FraudEngine.Api/              # HTTP layer — controllers, middleware, DI setup
  FraudEngine.Application/      # Use cases, orchestration, interfaces
  FraudEngine.Domain/           # Core models, rule evaluation, scoring logic
  FraudEngine.Infrastructure/   # Redis, SQL Server, Service Bus, external clients
  FraudEngine.Contracts/        # Request/response DTOs, event schemas

tests/
  FraudEngine.Unit/
  FraudEngine.Integration/
  FraudEngine.Load/             # k6 scripts
```

**Rule:** Domain and Application layers must have zero infrastructure dependencies. No Redis, SQL, or HTTP clients in `Domain` or `Application`.

---

## Naming

| Artifact | Convention | Example |
|----------|-----------|---------|
| Classes | PascalCase | `RuleEngine`, `FraudDecisionAudit` |
| Interfaces | `I` prefix + PascalCase | `IRuleEvaluator`, `IBehaviourStore` |
| Methods | PascalCase | `EvaluateAsync`, `ComputeRiskScore` |
| Private fields | `_camelCase` | `_ruleRepository` |
| Constants | SCREAMING_SNAKE | `MAX_RISK_SCORE`, `REVIEW_THRESHOLD` |
| DTOs | Noun + `Request` / `Response` | `EvaluateTransactionRequest`, `FraudDecisionResponse` |
| Events | Past-tense noun phrase | `FraudEvaluated`, `FraudRejected` |
| Redis keys | `noun:{id}:descriptor:window` | `user:{id}:txn_count:1min` |
| DB tables | PascalCase, plural | `FraudDecisions`, `RuleDefinitions` |

---

## Patterns to Follow

**Dependency inversion everywhere.** All infrastructure dependencies are injected via interfaces. The `Domain` layer never news up a Redis client.

**Result pattern for decisions.** Domain operations return a typed result (`FraudDecision`) — not exceptions for control flow.

```csharp
// Correct
public record FraudDecision(DecisionOutcome Outcome, int RiskScore, IReadOnlyList<string> Reasons);

// Wrong — do not throw for expected business outcomes
throw new FraudRejectedException("Velocity breach");
```

**One responsibility per class.** `RuleEngine` evaluates rules. `RiskScorer` computes the score. `DecisionEngine` maps score to outcome. These are separate classes.

**Explicit over implicit.** No magic strings. Reasons are defined as constants or an enum, not inline string literals.

```csharp
// Correct
RuleReasons.HighVelocity
RuleReasons.NewDevice

// Wrong
"HIGH_VELOCITY"  // inline anywhere except the constants file
```

**Async all the way down.** Every I/O call is awaited. No `.Result`, `.Wait()`, or `GetAwaiter().GetResult()` anywhere.

**Idempotency check first.** The first thing `EvaluateAsync` does is check whether `transactionId` already has a persisted decision. If yes — return it. No re-evaluation.

---

## Patterns Banned

| Pattern | Why |
|---------|-----|
| `Task.Result` / `.Wait()` | Deadlock risk on ASP.NET; violates async contract |
| Hardcoded thresholds in code | Thresholds live in config; code reads them |
| Raw SQL strings in application code | Use parameterised queries or an ORM; no string interpolation in SQL |
| Catching `Exception` broadly | Catch specific exceptions; let unhandled ones propagate to middleware |
| Storing PII fields in audit log | See `tradeoffs.md` — POPIA constraint |
| Boolean parameters | Use named options or enums; `Evaluate(true, false)` is unreadable |
| Static state | No static fields holding mutable state; everything is injected |
| Nested ternaries | Max one level; use `if/else` or pattern matching beyond that |

---

## Error Handling

- All unhandled exceptions are caught at the middleware layer and return an **RFC 7807 `ProblemDetails`** response — never a stack trace.
- Redis failures: catch `BehaviourStoreUnavailableException` (re-thrown from infra layer with Polly retries); log with correlation ID; continue with reduced scoring.
- Every error log must include `transactionId` and `correlationId` as structured fields.
- **Strict Typing:** All fraud rules must be categorized via the `RuleType` enum. Magic strings for rule identification are banned.

---

## Logging

```csharp
// Always include structured context
_logger.LogWarning("Redis unavailable for behaviour lookup {TransactionId}. Proceeding with reduced scoring.", transactionId);

// Never interpolate into log message strings — use structured parameters
_logger.LogInformation($"Decision: {decision}"); // WRONG
_logger.LogInformation("Decision: {Decision}", decision); // CORRECT
```

---

## Tests

- Every rule has a unit test that asserts the correct score delta for a matching input.
- Every rule has a unit test that asserts zero delta for a non-matching input.
- Integration tests use Testcontainers — no shared/external test databases.
- Minimum coverage target: **80% on Domain and Application layers**. Infrastructure layer is tested via integration tests, not mocked unit tests.
- No `Thread.Sleep` in tests. Use `Task.Delay` or deterministic fakes.
